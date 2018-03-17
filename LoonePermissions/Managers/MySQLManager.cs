using System;
using System.Collections.Generic;

using Rocket.API;
using Rocket.API.Serialisation;

using MySql.Data.MySqlClient;
using System.Text;
using System.Linq;
using System.Diagnostics;
using Rocket.Core.Assets;

namespace ChubbyQuokka.LoonePermissions.Managers
{
    internal static class MySqlManager
    {
        static MySqlConnection UnthreadedConnection;
        static MySqlConnection ThreadedConnection;

        static LoonePermissionsConfig._DatabaseSettings Settings => LoonePermissionsConfig.DatabaseSettings;

        static RocketPermissions internalPerms;
        static object internalPermsLock = new object();

        public static void Initialize()
        {
            UnthreadedConnection = new MySqlConnection(Queries.Connection);
            ThreadedConnection = new MySqlConnection(Queries.Connection);

            MySqlCommand cmd1 = CreateCommand();
            MySqlCommand cmd2 = CreateCommand();
            MySqlCommand cmd3 = CreateCommand();

            cmd1.CommandText = Queries.ShowTablesLikeGroupTable;
            cmd2.CommandText = Queries.ShowTablesLikePermissionTable;
            cmd3.CommandText = Queries.ShowTablesLikePlayerTable;

            OpenConnection();

            object obj1 = cmd1.ExecuteScalar();
            object obj2 = cmd2.ExecuteScalar();
            object obj3 = cmd3.ExecuteScalar();

            CloseConnection();

            if (obj1 == null)
            {
                OpenConnection();

                MySqlCommand cmd = UnthreadedConnection.CreateCommand();
                cmd.CommandText = Queries.CreateGroupTable;

                cmd.ExecuteNonQuery();

                UnthreadedConnection.Close();

                LoonePermissionsPlugin.Log($"Generating table: {Settings.GroupsTableName}", ConsoleColor.Yellow);

                //CreateGroup("default");
            }

            if (obj2 == null)
            {
                OpenConnection();

                MySqlCommand cmd = UnthreadedConnection.CreateCommand();
                cmd.CommandText = Queries.CreatePermissionTable;

                cmd.ExecuteNonQuery();

                CloseConnection();

                LoonePermissionsPlugin.Log($"Generating table: {Settings.PermissionsTableName}", ConsoleColor.Yellow);

                //AddPermission("default", "p", 0);
                //AddPermission("default", "rocket", 0);
                //AddPermission("default", "compass", 60);
            }

            if (obj3 == null)
            {
                OpenConnection();

                MySqlCommand cmd = CreateCommand();
                cmd.CommandText = Queries.CreatePlayerTable;

                cmd.ExecuteNonQuery();

                CloseConnection();

                LoonePermissionsPlugin.Log($"Generating table: {Settings.PlayerTableName}", ConsoleColor.Yellow);
            }

            if (obj1 == null && obj2 == null && obj3 == null)
            {
                Migrate();
            }
        }

        public static void Destroy()
        {
            if (UnthreadedConnection != null)
            {
                UnthreadedConnection.Dispose();
                UnthreadedConnection = null;
            }

            if (ThreadedConnection != null)
            {
                ThreadedConnection.Dispose();
                ThreadedConnection = null;
            }
        }

        internal static void Refresh()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            RocketPermissions temp = new RocketPermissions();

            var cmd1 = CreateCommand();
            var cmd2 = CreateCommand();
            var cmd3 = CreateCommand();

            cmd1.CommandText = Queries.SelectAllGroups;
            cmd2.CommandText = Queries.SelectAllPermissions;
            cmd3.CommandText = Queries.SelectAllPlayers;

            Dictionary<string, RocketPermissionsGroup> dict = new Dictionary<string, RocketPermissionsGroup>();

            OpenConnection();

            var dr1 = cmd1.ExecuteReader();

            while (dr1.Read())
            {
                RocketPermissionsGroup g = new RocketPermissionsGroup
                {
                    Members = new List<string>(),
                    Permissions = new List<Permission>(),
                    Id = dr1.GetString(0),
                    DisplayName = dr1.GetString(1),
                    ParentGroup = dr1.GetString(2),
                    Prefix = dr1.GetString(3),
                    Suffix = dr1.GetString(4),
                    Color = dr1.GetString(5),
                    Priority = dr1.GetInt16(6)
                };

                dict.Add(g.Id, g);
            }

            dr1.Close();
            dr1.Dispose();

            var dr2 = cmd2.ExecuteReader();

            while (dr2.Read())
            {
                string group = dr2.GetString(0);

                Permission p = new Permission(dr2.GetString(1), dr2.GetUInt32(2));

                if (dict.TryGetValue(group, out RocketPermissionsGroup g))
                {
                    g.Permissions.Add(p);
                }
            }

            dr2.Close();
            dr2.Dispose();

            var dr3 = cmd3.ExecuteReader();

            while (dr3.Read())
            {
                ulong cSteamId = dr3.GetUInt64(0);
                string group = dr3.GetString(1);

                if (dict.TryGetValue(group, out RocketPermissionsGroup g))
                {
                    g.Members.Add(cSteamId.ToString());
                }
            }

            dr3.Close();
            dr3.Dispose();

            CloseConnection();

            lock (internalPermsLock)
            {
                internalPerms = temp;
            }

            watch.Stop();

            string a = (ThreadedWorkManager.IsWorkerThread) ? "Worker" : "Main";

            LoonePermissionsPlugin.Log($"Reloading the cache took {watch.ElapsedMilliseconds} milliseconds on the {a} Thread!", ConsoleColor.Yellow);
        }

        static MySqlCommand CreateCommand()
        {
            if (ThreadedWorkManager.IsWorkerThread)
            {
                return ThreadedConnection.CreateCommand();
            }

            return UnthreadedConnection.CreateCommand();
        }

        static void OpenConnection()
        {
            if (ThreadedWorkManager.IsWorkerThread)
            {
                ThreadedConnection.Open();
            }
            else
            {
                UnthreadedConnection.Open();
            }
        }

        static void CloseConnection()
        {
            if (ThreadedWorkManager.IsWorkerThread)
            {
                ThreadedConnection.Close();
            }
            else
            {
                UnthreadedConnection.Close();
            }
        }

        public static RocketPermissionsProviderResult AddGroup(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult AddPlayerToGroup(string groupId, IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult DeleteGroup(string groupId)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsGroup GetGroup(string groupId)
        {
            throw new NotImplementedException();
        }

        public static List<RocketPermissionsGroup> GetGroups(IRocketPlayer player, bool includeParentGroups)
        {
            throw new NotImplementedException();
        }

        public static void GetGroups(List<RocketPermissionsGroup> groups, string[] ids, bool includeParent)
        {
            ids = ids.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();

            var cmd1 = CreateCommand();
            cmd1.CommandText = Queries.SelectGroupsByGroupIds(ids);

            OpenConnection();
            var dr1 = cmd1.ExecuteReader();

            List<string> parentsNeeded = new List<string>();

            while (dr1.Read())
            {
                RocketPermissionsGroup g = new RocketPermissionsGroup
                {
                    Id = dr1.GetString(0),
                    DisplayName = dr1.GetString(1),
                    ParentGroup = dr1.GetString(2),
                    Prefix = dr1.GetString(3),
                    Suffix = dr1.GetString(4),
                    Priority = dr1.GetInt16(5)
                };

                if (includeParent)
                {
                    var parent = groups.Where(x => x.Id == g.ParentGroup).FirstOrDefault();

                    if (parent == null)
                    {
                        parentsNeeded.Add(g.ParentGroup);
                    }
                }

                groups.Add(g);
            }

            dr1.Close();
            dr1.Dispose();

            CloseConnection();

            if (includeParent)
            {
                string[] parentsArray = parentsNeeded.Distinct().ToArray();

                if (parentsArray.Length != 0)
                {
                    GetGroups(groups, parentsArray, true);
                }
            }
        }

        public static RocketPermissionsProviderResult RemovePlayerFromGroup(string groupId, IRocketPlayer player)
        {
            if (IsPlayerInGroup(player, groupId))
            {
                var cmd1 = CreateCommand();

                cmd1.CommandText = Queries.DeletePlayerFromPlayers(player.DisplayName, groupId);

                OpenConnection();
                cmd1.ExecuteNonQuery();
                CloseConnection();

                return RocketPermissionsProviderResult.Success;
            }

            return RocketPermissionsProviderResult.UnspecifiedError;
        }

        //TODO
        public static RocketPermissionsProviderResult SaveGroup(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }

        public static List<string> GetPlayerGroupIds(IRocketPlayer player)
        {
            List<string> groups = new List<string>();
            groups.Add(LoonePermissionsConfig.DefaultGroup);

            var cmd1 = CreateCommand();

            cmd1.CommandText = Queries.SelectGroupIdsByPlayer(player.Id);

            OpenConnection();

            var dr1 = cmd1.ExecuteReader();

            while (dr1.Read())
            {
                groups.Add(dr1.GetString(0));
            }

            dr1.Close();
            dr1.Dispose();

            CloseConnection();

            return groups;
        }

        public static bool GroupExists(string groupId)
        {
            var cmd1 = CreateCommand();

            cmd1.CommandText = Queries.GroupExists(groupId);

            OpenConnection();
            object result = cmd1.ExecuteScalar();
            CloseConnection();

            return (bool)result;
        }

        public static bool IsPlayerInGroup(IRocketPlayer player, string groupId)
        {
            var cmd1 = CreateCommand();

            cmd1.CommandText = Queries.PlayerExists(player.Id, groupId);

            OpenConnection();
            object result = cmd1.ExecuteScalar();
            CloseConnection();

            return (bool)result;
        }

        public static bool Migrate()
        {
            try
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                RocketPermissions p = new XMLFileAsset<RocketPermissions>(Rocket.Core.Environment.PermissionFile, null, null).Instance;

                var cmd1 = CreateCommand();
                var cmd2 = CreateCommand();
                var cmd3 = CreateCommand();
                var cmd4 = CreateCommand();
                var cmd5 = CreateCommand();
                var cmd6 = CreateCommand();

                cmd1.CommandText = Queries.DeleteAllFromGroups;
                cmd2.CommandText = Queries.DeleteAllFromPlayers;
                cmd3.CommandText = Queries.DeleteAllFromPermissions;
                cmd4.CommandText = Queries.InsertGroupsIntoGroups(p.Groups);
                cmd5.CommandText = Queries.InsertGroupsIntoPlayers(p.Groups);
                cmd6.CommandText = Queries.InsertGroupsIntoPermissions(p.Groups);

                OpenConnection();

                cmd1.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                cmd3.ExecuteNonQuery();
                cmd4.ExecuteNonQuery();
                cmd5.ExecuteNonQuery();
                cmd6.ExecuteNonQuery();

                CloseConnection();

                LoonePermissionsConfig.SetDefaultGroup(p.DefaultGroup);

                timer.Stop();

                LoonePermissionsPlugin.Log($"Migration took {timer.ElapsedMilliseconds} milliseconds to complete!", ConsoleColor.Yellow);

                return true;
            }
            catch (Exception e)
            {
                LoonePermissionsPlugin.Log("Migration failed!", ConsoleColor.Red);
                LoonePermissionsPlugin.LogException(e);

                return false;
            }
        }

        static class Queries
        {
            public const string ValueSeperator = "', '";

            public static string Connection => $"SERVER={Settings.Address};DATABASE={Settings.Database};UID={Settings.Username};PASSWORD={Settings.Password};PORT={Settings.Port};";

            public static string CreateGroupTable => $"CREATE TABLE `{Settings.GroupsTableName}` (`groupid` VARCHAR(64) NOT NULL UNIQUE, `groupname` VARCHAR(64) NOT NULL, `parent` VARCHAR(64) NOT NULL, `prefix` VARCHAR(64) NOT NULL, `suffix` VARCHAR(64) NOT NULL, `color` VARCHAR(16) NOT NULL DEFAULT 'white', `priority` BIGINT NOT NULL DEFAULT '100', PRIMARY KEY (`groupid`))";
            public static string CreatePermissionTable => $"CREATE TABLE `{Settings.PermissionsTableName}` (`groupid` VARCHAR(64) NOT NULL, `permission` VARCHAR(64) NOT NULL, `cooldown` INTEGER NOT NULL DEFAULT '0')";
            public static string CreatePlayerTable => $"CREATE TABLE `{Settings.PlayerTableName}` (`csteamid` BIGINT NOT NULL, `groupid` VARCHAR(64) NOT NULL)";

            public static string ShowTablesLikeGroupTable => $"SHOW TABLES LIKE '{Settings.GroupsTableName}'";
            public static string ShowTablesLikePermissionTable => $"SHOW TABLES LIKE '{Settings.PermissionsTableName}'";
            public static string ShowTablesLikePlayerTable => $"SHOW TABLES LIKE '{Settings.PlayerTableName}'";

            public static string SelectAllGroups => $"SELECT * FROM `{Settings.GroupsTableName}` ORDER BY `priority` ASC";
            public static string SelectAllPermissions => $"SELECT * FROM `{Settings.PermissionsTableName}`";
            public static string SelectAllPlayers => $"SELECT * FROM `{Settings.PlayerTableName}`";

            public static string DeleteAllFromGroups => $"DELETE FROM `{Settings.GroupsTableName}`";
            public static string DeleteAllFromPermissions => $"DELETE FROM `{Settings.PermissionsTableName}`";
            public static string DeleteAllFromPlayers => $"DELETE FROM `{Settings.PlayerTableName}`";

            public static string DeleteGroupFromGroup(string groupId) => $"DELETE FROM `{Settings.GroupsTableName}` WHERE `groupid` = '{groupId}'";
            public static string DeleteGroupFromPermissions(string groupId) => $"DELETE FROM `{Settings.PermissionsTableName}` WHERE `groupid` = '{groupId}'";
            public static string DeleteGroupFromPlayers(string groupId) => $"DELETE FROM `{Settings.PlayerTableName}` WHERE `groupid` = '{groupId}'";

            public static string SelectGroupIdsByPlayer(string steamId) => $"SELECT `groupid` FROM `{Settings.PlayerTableName}` WHERE `csteamid` = '{steamId}'";
            public static string SelectPermissionsByGroupId(string groupId) => $"SELECT `permission`, `cooldown` FROM `{Settings.PermissionsTableName} WHERE `groupid` = '{groupId}'";
            public static string SelectGroupByGroupId(string groupId) => $"SELECT `groupname`, `parent`, `prefix`, `suffix`, `color`, `priority` FROM `{Settings.GroupsTableName}` WHERE `groupid` = '{groupId}'";

            public static string InsertPlayerToPlayers(string steamId, string groupId) => $"INSERT INTO `{Settings.PlayerTableName}` VALUES ('{steamId}', '{groupId}')";
            public static string DeletePlayerFromPlayers(string steamId, string groupId) => $"DELETE FROM `{Settings.PlayerTableName}'` WHERE `steamid` = '{steamId}' AND `groupid` = '{groupId}'";

            public static string GroupExists(string groupId) => $"SELECT EXISTS (SELECT * FROM `{Settings.GroupsTableName}` WHERE `groupid` = '{groupId}')";
            public static string PermissionExists(string permission, string groupId) => $"SELECT EXISTS (SELECT * FROM `{Settings.PermissionsTableName}` WHERE `groupid` = '{groupId}' AND `permission` = '{permission}')";
            public static string PlayerExists(string steamId, string groupId) => $"SELECT EXISTS (SELECT * FROM `{Settings.PlayerTableName}` WHERE `groupid` = '{groupId}' AND `csteamid` = '{steamId}')";

            public static string UpdateGroup(string groupId, string param, string value) => $"UPDATE `{Settings.GroupsTableName}` SET `{param}` = '{value}' WHERE `groupid` = '{groupId}'";
            public static string UpdatePermission(string groupId, string permission, string newCooldown) => $"UPDATE `{Settings.PermissionsTableName}` SET `cooldown` = '{newCooldown}' WHERE `groupid` = '{groupId}' AND `permission` = '{permission}'";

            #region Select Specific Groups
            public static string SelectGroupsByGroupIds(string[] ids)
            {
                if (ids != null)
                {
                    ids = ids.Where(x => x != null).ToArray();

                    if (ids.Length != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"SELECT * FROM ");
                        sb.Append(Settings.GroupsTableName);
                        sb.Append(" WHERE");

                        for (int i = 0; i < ids.Length; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(" OR");
                            }

                            sb.Append(" `groupid` = '");
                            sb.Append(ids[i]);
                            sb.Append("'");
                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(ids));
                }

                throw new ArgumentNullException(nameof(ids));
            }

            public static string SelectPlayersByGroupIds(string[] ids)
            {
                if (ids != null)
                {
                    ids = ids.Where(x => x != null).ToArray();

                    if (ids.Length != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"SELECT * FROM ");
                        sb.Append(Settings.PlayerTableName);
                        sb.Append(" WHERE");

                        for (int i = 0; i < ids.Length; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(" OR");
                            }

                            sb.Append(" `groupid` = '");
                            sb.Append(ids[i]);
                            sb.Append("'");
                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(ids));
                }

                throw new ArgumentNullException(nameof(ids));
            }

            public static string SelectPermissionsByGroupIds(string[] ids)
            {
                if (ids != null)
                {
                    ids = ids.Where(x => x != null).ToArray();

                    if (ids.Length != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"SELECT * FROM ");
                        sb.Append(Settings.PermissionsTableName);
                        sb.Append(" WHERE");

                        for (int i = 0; i < ids.Length; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(" OR");
                            }

                            sb.Append(" `groupid` = '");
                            sb.Append(ids[i]);
                            sb.Append("'");
                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(ids));
                }

                throw new ArgumentNullException(nameof(ids));
            }
            #endregion

            #region Migration
            public static string InsertGroupsIntoGroups(List<RocketPermissionsGroup> groups)
            {
                if (groups != null)
                {
                    groups = groups.Where(x => x != null).ToList();

                    if (groups.Count != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"INSERT INTO `");
                        sb.Append(Settings.GroupsTableName);
                        sb.Append("` VALUES");

                        for (int i = 0; i < groups.Count; i++)
                        {
                            RocketPermissionsGroup g = groups[i];

                            if (i != 0)
                            {
                                sb.Append(",");
                            }

                            sb.Append(" ('");
                            sb.Append(g.Id);
                            sb.Append(ValueSeperator);
                            sb.Append(g.DisplayName);
                            sb.Append(ValueSeperator);
                            sb.Append(g.ParentGroup);
                            sb.Append(ValueSeperator);
                            sb.Append(g.Prefix);
                            sb.Append(ValueSeperator);
                            sb.Append(g.Suffix);
                            sb.Append(ValueSeperator);
                            sb.Append(g.Color);
                            sb.Append(ValueSeperator);
                            sb.Append(g.Priority);
                            sb.Append("')");

                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(groups));
                }

                throw new ArgumentNullException(nameof(groups));
            }

            public static string InsertGroupsIntoPlayers(List<RocketPermissionsGroup> groups)
            {
                if (groups != null)
                {
                    groups = groups.Where(x => x != null).ToList();

                    if (groups.Count != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"INSERT INTO `");
                        sb.Append(Settings.PlayerTableName);
                        sb.Append("` VALUES");

                        //Thank you .NET 3.5 for not having Tuples.
                        List<string> Players = new List<string>();
                        List<string> Groups = new List<string>();

                        for (int i = 0; i < groups.Count; i++)
                        {
                            if (groups[i].Members != null && groups[i].Members.Count != 0)
                            {
                                groups[i].Members = groups[i].Members.Where(x => x != null).ToList();

                                for (int ii = 0; ii < groups[i].Members.Count; ii++)
                                {
                                    Players.Add(groups[i].Members[ii]);
                                    Groups.Add(groups[i].Id);
                                }
                            }
                        }

                        for (int i = 0; i < Players.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(",");
                            }

                            sb.Append(" ('");
                            sb.Append(Players[i]);
                            sb.Append(ValueSeperator);
                            sb.Append(Groups[i]);
                            sb.Append("')");
                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(groups));
                }

                throw new ArgumentNullException(nameof(groups));
            }

            public static string InsertGroupsIntoPermissions(List<RocketPermissionsGroup> groups)
            {
                if (groups != null)
                {
                    groups = groups.Where(x => x != null).ToList();

                    if (groups.Count != 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.Append($"INSERT INTO `");
                        sb.Append(Settings.PermissionsTableName);
                        sb.Append("` VALUES");

                        //Again, thank you .NET 3.5 for not having Tuples.
                        List<Permission> Permissions = new List<Permission>();
                        List<string> Groups = new List<string>();

                        for (int i = 0; i < groups.Count; i++)
                        {
                            if (groups[i].Permissions != null && groups[i].Permissions.Count != 0)
                            {
                                groups[i].Permissions = groups[i].Permissions.Where(x => x != null).ToList();

                                for (int ii = 0; ii < groups[i].Permissions.Count; ii++)
                                {
                                    Permissions.Add(groups[i].Permissions[ii]);
                                    Groups.Add(groups[i].Id);
                                }
                            }
                        }

                        for (int i = 0; i < Permissions.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(",");
                            }

                            sb.Append(" ('");
                            sb.Append(Groups[i]);
                            sb.Append(ValueSeperator);
                            sb.Append(Permissions[i].Name);
                            sb.Append(ValueSeperator);
                            sb.Append(Permissions[i].Cooldown);
                            sb.Append("')");
                        }

                        return sb.ToString();
                    }

                    throw new ArgumentException("The array must contain at least one non-null member!", nameof(groups));
                }

                throw new ArgumentNullException(nameof(groups));
            }
            #endregion
        }
    }
}
/*
    public static class MySqlManager
    {
        static string GROUP_TABLE;
        static string PERMISSION_TABLE;
        static string PLAYER_TABLE;

        static string GROUP_TABLE_CREATE => "CREATE TABLE `" + GROUP_TABLE + "` (`groupid` VARCHAR(64) NOT NULL UNIQUE, `groupname` VARCHAR(64) NOT NULL, `parent` VARCHAR(64), `prefix` VARCHAR(64), `suffix` VARCHAR(64), `color` VARCHAR(7) DEFAULT 'white', `priority` BIGINT DEFAULT '100', PRIMARY KEY (`groupid`))";
        static string PERMISSION_TABLE_CREATE => "CREATE TABLE `" + PERMISSION_TABLE + "` (`groupid` VARCHAR(64) NOT NULL, `permission` VARCHAR(64) NOT NULL, `cooldown` INTEGER DEFAULT '0')";
        static string PLAYER_TABLE_CREATE => "CREATE TABLE `" + PLAYER_TABLE + "` (`csteamid` BIGINT NOT NULL, `groupid` VARCHAR(64) NOT NULL)";

        static MySqlConnection Connection => connection;
        static MySqlConnection connection;

        public static void Initialize()
        {
            GROUP_TABLE = LoonePermissionsConfig.DatabaseSettings.GroupsTableName;
            PERMISSION_TABLE = LoonePermissionsConfig.DatabaseSettings.PermissionsTableName;
            PLAYER_TABLE = LoonePermissionsConfig.DatabaseSettings.PlayerTableName;

            connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", LoonePermissionsConfig.DatabaseSettings.Address, LoonePermissionsConfig.DatabaseSettings.Database, LoonePermissionsConfig.DatabaseSettings.Username, LoonePermissionsConfig.DatabaseSettings.Password, LoonePermissionsConfig.DatabaseSettings.Port));

            MySqlCommand cmd1 = connection.CreateCommand();
            MySqlCommand cmd2 = connection.CreateCommand();
            MySqlCommand cmd3 = connection.CreateCommand();

            cmd1.CommandText = string.Format("SHOW TABLES LIKE '{0}'", GROUP_TABLE);
            cmd2.CommandText = string.Format("SHOW TABLES LIKE '{0}'", PERMISSION_TABLE);
            cmd3.CommandText = string.Format("SHOW TABLES LIKE '{0}'", PLAYER_TABLE);

            TryOpen();

            object obj1 = cmd1.ExecuteScalar();
            object obj2 = cmd2.ExecuteScalar();
            object obj3 = cmd3.ExecuteScalar();

            TryClose();

            if (obj1 == null) {
                TryOpen();

                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = GROUP_TABLE_CREATE;
                cmd.ExecuteNonQuery();
                TryClose();

                Logger.Log(string.Format("Generating table: {0}", GROUP_TABLE), ConsoleColor.Yellow);

                CreateGroup("default");
            }

            if (obj2 == null) {
                TryOpen();

                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = PERMISSION_TABLE_CREATE;
                cmd.ExecuteNonQuery();

                TryClose();

                Logger.Log(string.Format("Generating table: {0}", PERMISSION_TABLE), ConsoleColor.Yellow);

                AddPermission("default", "p", 0);
                AddPermission("default", "rocket", 0);
                AddPermission("default", "compass", 60);
            }

            if (obj3 == null) {
                TryOpen();

                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = PLAYER_TABLE_CREATE;
                cmd.ExecuteNonQuery();

                TryClose();

                Logger.Log(string.Format("Generating table: {0}", PLAYER_TABLE), ConsoleColor.Yellow);
            }
        }

        public static RocketPermissionsGroup GetGroup(string groupId)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("SELECT * FROM `{0}` WHERE `groupid`='{1}'", GROUP_TABLE, groupId);

            TryOpen();
            MySqlDataReader dr1 = cmd1.ExecuteReader();

            RocketPermissionsGroup group;

            if (dr1.Read()) {

                group = new RocketPermissionsGroup()
                {
                    Id = groupId,
                    DisplayName = (dr1.IsDBNull(1)) ? "" : dr1.GetString(1),
                    ParentGroup = (dr1.IsDBNull(2)) ? "" : dr1.GetString(2),
                    Prefix = (dr1.IsDBNull(3)) ? "" : dr1.GetString(3),
                    Suffix = (dr1.IsDBNull(4)) ? "" : dr1.GetString(4),
                    Color = (dr1.IsDBNull(5)) ? "" : dr1.GetString(5),
                    Priority = (dr1.IsDBNull(6)) ? (short)100 : dr1.GetInt16(6)
                };
                TryClose();

                TryOpen();
                group.Members = new List<string>();
                MySqlCommand cmd2 = Connection.CreateCommand();
                cmd2.CommandText = string.Format("SELECT `csteamid` FROM `{0}` WHERE `groupid`='{1}'", PLAYER_TABLE, groupId);
                MySqlDataReader dr2 = cmd2.ExecuteReader();
                while (dr2.Read()) {
                    group.Members.Add(dr2.GetString(0));
                }
                TryClose();

                TryOpen();
                group.Permissions = new List<Permission>();
                MySqlCommand cmd3 = Connection.CreateCommand();
                cmd3.CommandText = string.Format("SELECT * FROM `{0}` WHERE `groupid`='{1}'", PERMISSION_TABLE, groupId);
                MySqlDataReader dr3 = cmd3.ExecuteReader();
                while (dr3.Read()) {
                    group.Permissions.Add(new Permission(dr3.GetString(1), dr3.GetUInt32(2)));
                }
                TryClose();

            } else {
                TryClose();
                return null; // no such group eh
            }

            return group;
        }

        public static void TryOpen()
        {
            try {
                Connection.Open();
            } catch {
                Logger.Log("LoonePermissions caught a connection error! Just ignore this message :D", ConsoleColor.Yellow);
                Connection.Close();
                Connection.Open();
            }
        }

        public static void TryClose()
        {
            try {
                Connection.Close();
            } catch {
                Logger.Log("LoonePermissions caught a connection error! Just ignore this message :D", ConsoleColor.Yellow);
            }
        }

        public static void CreateGroup(string groupId)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();
            cmd1.CommandText = string.Format("INSERT INTO `{0}` VALUES ('{1}','{1}','{2}','{2}','{2}','{3}','{4}')", GROUP_TABLE, groupId, null, "white", 100);

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();
        }

        public static void DeleteGroup(string groupId)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();
            MySqlCommand cmd2 = Connection.CreateCommand();
            MySqlCommand cmd3 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("DELETE FROM `{0}` WHERE `groupid`='{1}'", GROUP_TABLE, groupId);
            cmd2.CommandText = string.Format("DELETE FROM `{0}` WHERE `groupid`='{1}'", PERMISSION_TABLE, groupId);
            cmd3.CommandText = string.Format("UPDATE `{0}` SET `parent`='{1}' WHERE `parent`='{2}'", GROUP_TABLE, LoonePermissionsConfig.DefaultGroup, groupId);

            ReassignTo(groupId, LoonePermissionsConfig.DefaultGroup.ToLower(), false);

            TryOpen();
            cmd1.ExecuteNonQuery();
            cmd2.ExecuteNonQuery();
            cmd3.ExecuteNonQuery();
            TryClose();
        }

        public static bool UpdateGroup(EGroupProperty prop, string value, string groupId)
        {
            bool wasSuccess = false;
            MySqlCommand cmd1 = Connection.CreateCommand();

            switch (prop) {
                case EGroupProperty.COLOR:
                    if (IsValidColor(value, out string newVal)) {
                        wasSuccess = true;
                        cmd1.CommandText = string.Format("UPDATE `{0}` SET `color`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, newVal, groupId);
                    } else
                        wasSuccess = false;
                    break;
                case EGroupProperty.NAME:
                    cmd1.CommandText = string.Format("UPDATE `{0}` SET `groupname`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
                    wasSuccess = true;
                    break;
                case EGroupProperty.PARENT:
                    cmd1.CommandText = string.Format("UPDATE `{0}` SET `parent`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
                    wasSuccess = true;
                    break;
                case EGroupProperty.PREFIX:
                    cmd1.CommandText = string.Format("UPDATE `{0}` SET `prefix`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
                    wasSuccess = true;
                    break;
                case EGroupProperty.SUFFIX:
                    cmd1.CommandText = string.Format("UPDATE `{0}` SET `suffix`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
                    wasSuccess = true;
                    break;
                case EGroupProperty.PRIORITY:
                    if (short.TryParse(value, out short i)) {
                        cmd1.CommandText = string.Format("UPDATE `{0}` SET `priority`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
                        wasSuccess = true;
                    } else
                        wasSuccess = false;

                    break;
            }

            if (!wasSuccess)
                return false;

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();

            return true;
        }

        public static void ReassignTo(string x, string y, bool require)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            //Why was this originally not like this?
            cmd1.CommandText = string.Format("SELECT `csteamid` FROM `{0}` WHERE `groupid`='{1}'", PLAYER_TABLE, x);

            List<ulong> ids = new List<ulong>();

            TryOpen();
            MySqlDataReader dr = cmd1.ExecuteReader();
            while (dr.Read())
                ids.Add(dr.GetUInt64(0));
            TryClose();

            foreach (ulong id in ids) {
                RemovePlayerFromGroup(id, x);
                if (require)
                    AddPlayerToGroup(id, y);
            }
        }

        static bool IsValidColor(string value, out string mod)
        {
            string color = value.ToLower();

            mod = value;

            if (value == "black")
                return true;

            if (value == "blue")
                return true;

            if (value == "clear")
                return true;

            if (value == "cyan")
                return true;

            if (value == "gray")
                return true;

            if (value == "green")
                return true;

            if (value == "grey")
                return true;

            if (value == "magenta")
                return true;

            if (value == "red")
                return true;

            if (value == "white")
                return true;

            if (value == "yellow")
                return true;

            string newVal = "";
            if (!value.StartsWith("#", StringComparison.Ordinal))
                newVal = "#" + value;
            else
                newVal = value;

            mod = newVal;

            return newVal.Length == 7;
        }

        public static List<Permission> GetPermissionsByGroup(string groupId, bool includeParents)
        {
            List<Permission> perms = new List<Permission>();

            MySqlCommand cmd1 = Connection.CreateCommand();
            MySqlCommand cmd2 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("SELECT `permission`, `cooldown` FROM `{0}` WHERE `groupid`='{1}'", PERMISSION_TABLE, groupId);
            cmd2.CommandText = string.Format("SELECT `parent` FROM `{0}` WHERE `groupid`='{1}'", GROUP_TABLE, groupId);

            if (includeParents) {
                TryOpen();
                object obj = cmd2.ExecuteScalar();

                string parent = (obj == null) ? "" : obj.ToString();

                TryClose();

                if (parent != "")
                    perms.AddRange(GetPermissionsByGroup(parent, true));
            }

            TryOpen();
            MySqlDataReader dr = cmd1.ExecuteReader();

            while (dr.Read()) {
                perms.Add(new Permission(dr.GetString(0), dr.GetUInt32(1)));
            }
            TryClose();

            return perms;
        }

        public static string[] GetPlayerGroups(ulong steamid, bool forceDefault)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();
            cmd1.CommandText = string.Format("SELECT `groupid` FROM `{0}` WHERE `csteamid`='{1}'", PLAYER_TABLE, steamid);

            List<string> groups = new List<string>();

            TryOpen();
            MySqlDataReader dr = cmd1.ExecuteReader();

            while (dr.Read()) {
                groups.Add(dr.GetString(0));
            }

            TryClose();

            if (groups.Count == 0) {
                groups.Add(LoonePermissionsConfig.DefaultGroup.ToLower());
            }


            return groups.ToArray();
        }

        public static bool GroupExists(string groupId)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();
            cmd1.CommandText = string.Format("SELECT `groupid` FROM `{0}` WHERE `groupid`='{1}'", GROUP_TABLE, groupId);

            TryOpen();
            string group = (string)cmd1.ExecuteScalar();
            TryClose();

            return group != null;
        }

        public static RocketPermissionsProviderResult AddPlayerToGroup(ulong player, string groupId)
        {
            if (!GroupExists(groupId))
                return RocketPermissionsProviderResult.GroupNotFound;

            string[] currentGroup = GetPlayerGroups(player, true);

            if (groupId == LoonePermissionsConfig.DefaultGroup && currentGroup[0] == LoonePermissionsConfig.DefaultGroup) {
                return RocketPermissionsProviderResult.Success;
            }

            for (int i = 0; i < currentGroup.Length; i++) {
                if (groupId == currentGroup[i]) {
                    return RocketPermissionsProviderResult.UnspecifiedError;
                }
            }

            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("INSERT INTO `{0}` VALUES ('{1}','{2}')", PLAYER_TABLE, player, groupId.ToLower());

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();

            return RocketPermissionsProviderResult.Success;
        }

        public static RocketPermissionsProviderResult RemovePlayerFromGroup(ulong player, string groupId)
        {
            string[] currentGroup = GetPlayerGroups(player, true);
            int index;

            if (currentGroup.Length != 1) {
                for (int i = 0; i < currentGroup.Length; i++) {
                    if (groupId == currentGroup[i]) {
                        index = i;
                        goto SUCCESS;
                    }
                }
            } else {
                AddPlayerToGroup(player, LoonePermissionsConfig.DefaultGroup);
            }

            if (currentGroup.Length == 2) {
                if (currentGroup[0] == LoonePermissionsConfig.DefaultGroup || currentGroup[1] == LoonePermissionsConfig.DefaultGroup) {
                    RemovePlayerFromGroup(player, LoonePermissionsConfig.DefaultGroup);
                }
            }

        SUCCESS:

            MySqlCommand cmd2 = Connection.CreateCommand();

            cmd2.CommandText = string.Format("DELETE FROM `{0}` WHERE `csteamid`='{1}' AND `groupid`='{2}'", PLAYER_TABLE, player, groupId);

            TryOpen();
            cmd2.ExecuteNonQuery();
            TryClose();

            return RocketPermissionsProviderResult.Success;
        }

        public static int GetPermission(string perm, string groupId)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("SELECT `cooldown` FROM `{0}` WHERE `permission`='{1}' AND `groupid`='{2}'", PERMISSION_TABLE, perm.ToLower(), groupId);

            TryOpen();
            object obj = cmd1.ExecuteScalar();
            TryClose();

            return (obj == null) ? -1 : (int)obj;
        }

        public static void AddPermission(string group, string perm, int cooldown)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("INSERT INTO `{0}` VALUES ('{1}','{2}','{3}')", PERMISSION_TABLE, group, perm, cooldown);

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();
        }


        public static void RemovePermission(string group, string perm)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("DELETE FROM `{0}` WHERE `permission`='{1}' AND `groupid`='{2}'", PERMISSION_TABLE, perm, group);

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();
        }

        public static void ModifyPermsission(string group, string perm, int cooldown)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("UPDATE `{0}` SET `cooldown`='{1}' WHERE `permission`='{2}' AND `groupid`='{3}'", PERMISSION_TABLE, cooldown, perm, group);

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();
        }

        public static bool PlayerExists(ulong player)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("SELECT `csteamid` FROM `{0}` WHERE `csteamid`='{1}'", PLAYER_TABLE, player);

            TryOpen();
            string i = (string)cmd1.ExecuteScalar();
            TryClose();

            return i != null;
        }

        public static void MigrateDatabase()
        {

            MySqlCommand cmd1 = Connection.CreateCommand();
            MySqlCommand cmd2 = Connection.CreateCommand();
            MySqlCommand cmd3 = Connection.CreateCommand();

            cmd1.CommandText = String.Format("DELETE FROM {0}", PLAYER_TABLE);
            cmd2.CommandText = String.Format("DELETE FROM {0}", PERMISSION_TABLE);
            cmd3.CommandText = String.Format("DELETE FROM {0}", GROUP_TABLE);

            TryOpen();
            cmd1.ExecuteNonQuery();
            cmd2.ExecuteNonQuery();
            cmd3.ExecuteNonQuery();
            TryClose();

            RocketPermissions p = new XMLFileAsset<RocketPermissions>(Rocket.Core.Environment.PermissionFile, null, null).Instance;
            LoonePermissionsConfig.SetDefaultGroup(p.DefaultGroup);

            foreach (RocketPermissionsGroup group in p.Groups) {
                CreateGroup(group.Id);
                if (group.Color != null) UpdateGroup(EGroupProperty.COLOR, group.Color, group.Id);
                if (group.ParentGroup != null) UpdateGroup(EGroupProperty.PARENT, group.ParentGroup, group.Id);
                if (group.Prefix != null) UpdateGroup(EGroupProperty.PREFIX, group.Prefix, group.Id);
                if (group.Suffix != null) UpdateGroup(EGroupProperty.SUFFIX, group.Suffix, group.Id);
                if (group.DisplayName != null) UpdateGroup(EGroupProperty.NAME, group.DisplayName, group.Id);
                UpdateGroup(EGroupProperty.PRIORITY, group.Priority.ToString(), group.Id);

                foreach (Permission perm in group.Permissions) {
                    string name = perm.Name;
                    name = name.Replace("-", "~");

                    AddPermission(group.Id, name, (int)perm.Cooldown);
                }

                foreach (string player in group.Members) {
                    if (ulong.TryParse(player, out ulong id)) {
                        AddPlayerToGroup(id, group.Id);
                    }
                }
            }
        }
    }
    */
