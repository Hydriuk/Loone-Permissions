using System;

using Rocket.API.Serialisation;

using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using LoonePermissions.API;

namespace LoonePermissions.Managers
{
    public static class MySqlManager
    {

        const string GROUP_TABLE = "loone_groups";
        const string PERMISSION_TABLE = "loone_permissions";
        const string PLAYER_TABLE = "loone_players";

        const string GROUP_TABLE_CREATE = "CREATE TABLE `" + GROUP_TABLE + "` (`groupid` VARCHAR(50) NOT NULL UNIQUE, `groupname` VARCHAR(50) NOT NULL, `parent` VARCHAR(50), `prefix` VARCHAR(50), `suffix` VARCHAR(50), `color` VARCHAR(7) DEFAULT 'white', `priority` BIGINT DEFAULT '100', PRIMARY KEY (`groupid`))";
        const string PERMISSION_TABLE_CREATE = "CREATE TABLE `" + PERMISSION_TABLE + "` (`groupid` VARCHAR(50) NOT NULL, `permission` VARCHAR(50) NOT NULL, `cooldown` INTEGER DEFAULT '0')";
        const string PLAYER_TABLE_CREATE = "CREATE TABLE `" + PLAYER_TABLE + "` (`csteamid` BIGINT NOT NULL, `groupid` VARCHAR(255) NOT NULL)";

        static MySqlConnection Connection => connection;
        static MySqlConnection connection;

        public static void Initialize()
        {
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
            MySqlDataReader dr = cmd1.ExecuteReader();

            RocketPermissionsGroup group;

            if (dr.Read()) {
                group = new RocketPermissionsGroup()
                {
                    Id = groupId,
                    DisplayName = (dr.IsDBNull(1)) ? "" : dr.GetString(1),
                    ParentGroup = (dr.IsDBNull(2)) ? "" : dr.GetString(2),
                    Prefix = (dr.IsDBNull(3)) ? "" : dr.GetString(3),
                    Suffix = (dr.IsDBNull(4)) ? "" : dr.GetString(4),
                    Color = (dr.IsDBNull(5)) ? "" : dr.GetString(5),
                    Priority = (dr.IsDBNull(6)) ? (short)100 : dr.GetInt16(6)
                };
                TryClose();
                //Is this even needed?
                //group.Permissions = GetPermissionsByGroup(groupId, true);
            } else {
                TryClose();
                return GetGroup(LoonePermissionsConfig.DefaultGroup.ToLower());
            }

            return group;
        }

        public static void TryOpen()
        {
            try{
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
                    if (IsValidColor(value)) {
                        wasSuccess = true;
                        cmd1.CommandText = string.Format("UPDATE `{0}` SET `color`='{1}' WHERE `groupid`='{2}'", GROUP_TABLE, value, groupId);
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

            cmd1.CommandText = string.Format("SELECT `csteamid` FROM `{0}`", PLAYER_TABLE, y, x);

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

        static bool IsValidColor(string value)
        {
            string color = value.ToLower();

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

            return value.StartsWith("#", StringComparison.Ordinal);
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
        
        public static string[] GetPlayerGroups(ulong steamid)
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
                AddPlayerFirstTime(steamid);
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

        public static void AddPlayerFirstTime(ulong player)
        {
            MySqlCommand cmd1 = Connection.CreateCommand();

            cmd1.CommandText = string.Format("INSERT INTO `{0}` VALUES ('{1}','{2}')", PLAYER_TABLE, player, LoonePermissionsConfig.DefaultGroup.ToLower());

            TryOpen();
            cmd1.ExecuteNonQuery();
            TryClose();
        }
        
        public static RocketPermissionsProviderResult AddPlayerToGroup(ulong player, string groupId)
        {
            if (!GroupExists(groupId))
                return RocketPermissionsProviderResult.GroupNotFound;

            string[] currentGroup = GetPlayerGroups(player);

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
            string[] currentGroup = GetPlayerGroups(player);
            int index;

            if (currentGroup.Length != 1) {

                for (int i = 0; i < currentGroup.Length; i++) {
                    if (groupId == currentGroup[i]) {
                        index = i;
                        goto SUCCESS;
                    }
                }
            }

            return RocketPermissionsProviderResult.UnspecifiedError;

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

            cmd1.CommandText = string.Format("SELECT `cooldown` FROM `{0}` WHERE `permission`='{1}'", PERMISSION_TABLE, perm.ToLower());

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
    }
}