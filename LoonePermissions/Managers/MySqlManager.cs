using System;

using Rocket.API.Serialisation;

using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using LoonePermissions.API;
using Rocket.Core.Assets;

namespace LoonePermissions.Managers
{
    public static class MySqlManager
    {

        const string GROUP_TABLE = "loone_groups";
        const string PERMISSION_TABLE = "loone_permissions";
        const string PLAYER_TABLE = "loone_players";

        const string GROUP_TABLE_CREATE = "CREATE TABLE `" + GROUP_TABLE + "` (`groupid` VARCHAR(64) NOT NULL UNIQUE, `groupname` VARCHAR(64) NOT NULL, `parent` VARCHAR(64), `prefix` VARCHAR(64), `suffix` VARCHAR(64), `color` VARCHAR(7) DEFAULT 'white', `priority` BIGINT DEFAULT '100', PRIMARY KEY (`groupid`))";
        const string PERMISSION_TABLE_CREATE = "CREATE TABLE `" + PERMISSION_TABLE + "` (`groupid` VARCHAR(64) NOT NULL, `permission` VARCHAR(64) NOT NULL, `cooldown` INTEGER DEFAULT '0')";
        const string PLAYER_TABLE_CREATE = "CREATE TABLE `" + PLAYER_TABLE + "` (`csteamid` BIGINT NOT NULL, `groupid` VARCHAR(64) NOT NULL)";

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
                group.Permissions=new List<Permission>();
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
            if (!value.StartsWith("#"))
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
            if (forceDefault) {

                if (groups.Count == 0) {
                    groups.Add(LoonePermissionsConfig.DefaultGroup.ToLower());
                    AddPlayerFirstTime(steamid);
                }
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

            string[] currentGroup = GetPlayerGroups(player, false);

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
}