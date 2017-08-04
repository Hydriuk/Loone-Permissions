using System;
using System.IO;
using System.Collections.Generic;

using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Plugins;

using Newtonsoft.Json;
using RocketLogger = Rocket.Core.Logging.Logger;

using LoonePermissions.Providers;
using Rocket.Core;
using LoonePermissions.Managers;
using Rocket.Unturned.Player;
using Rocket.Unturned;

using UnityEngine;
using Rocket.Unturned.Chat;
using Rocket.API.Collections;

namespace LoonePermissions
{
    public class LoonePermissions : RocketPlugin
    {
        public static LoonePermissions instance;
        static IRocketPermissionsProvider orignal;

        protected override void Load()
        {
            instance = this;

            RocketLogger.Log(string.Format("Welcome to Loone Permissions v{0}!", Assembly.GetName().Version.ToString()), ConsoleColor.Yellow);

            LoonePermissionsConfig.Initialize();
            MySqlManager.Initialize();
            CommandManager.Initialize();

            orignal = R.Permissions;
            R.Permissions = new MySqlProvider();
        }

        protected override void Unload()
        {
            R.Permissions = orignal;
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList {
                    { "invalid_args", "You have specified an invalid arguement!" },
                    { "invalid_perms", "You don't have permission to do this!" },
                    { "invalid_cmd", "That commands doesn't exist!" },
                    { "invalid_color", "You specified an invalid color!" },
                    { "group_create", "You have created the group: {0}!" },
                    { "group_delete", "You have deleted the group: {0}, all players in this group were moved to {1}!" },
                    { "group_delete_default", "You can't delete the default group!" },
                    { "group_default", "You have changed the default group to {0}!" },
                    { "group_default_already", "This is already the default group!" },
                    { "group_exists", "That group already exists!" },
                    { "group_not_exists", "That group doesn't exists!" },
                    { "group_modified", "You have set the {0} of {1} to {2}!" },
                    { "perm_added", "The permission {0} has been added to {1} with a cooldown of {2}!" },
                    { "perm_removed", "The permission {0} has been removed from {1}!" },
                    { "perm_exists", "The group {0} already has the permission {1} with a cooldown of {2}! " },
                    { "perm_not_exists", "The group {0} doesn't have the permission {1}!" },
                    { "perm_modified", "The cooldown for {0} in the group {1} has been set to {2}!"}
                };
            }
        }

        public static void Say(IRocketPlayer caller, string message, Color color, params object[] objs)
        {
            UnturnedChat.Say(caller, instance.Translate(message, objs), color);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LoonePermissionsConfig
    {

        static string Directory = Rocket.Core.Environment.PluginsDirectory + "/LoonePermissions/Config.json";
        static string Directory_Errored = Rocket.Core.Environment.PluginsDirectory + "/LoonePermissions/Config_Errored.json";

        static LoonePermissionsConfig instance;

        public static string DefaultGroup => instance.defaultGroup;
        public static MySqlSettings DatabaseSettings => instance.databaseSettings;

        [JsonProperty(PropertyName = "DefaultGroup")]
        string defaultGroup;

        [JsonProperty(PropertyName = "MySQLSettings")]
        MySqlSettings databaseSettings;

        public static void Initialize()
        {
            if (File.Exists(Directory)) {
                string config = File.ReadAllText(Directory);
                try {
                    instance = JsonConvert.DeserializeObject<LoonePermissionsConfig>(config);
                } catch {
                    File.WriteAllText(Directory_Errored, config);
                    LoadDefaultConfig();
                    SaveConfig();
                    RocketLogger.LogError("Config failed to load! Reverting to default settings...");
                }
            } else {
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(instance, Formatting.Indented);
            File.WriteAllText(Directory, json);
        }

        static void LoadDefaultConfig()
        {
            instance = new LoonePermissionsConfig();
            instance.defaultGroup = "default";
            instance.databaseSettings = new MySqlSettings { Database = "unturned", Username = "root", Password = "toor", Address = "127.0.0.1", Port = 3306 };
        }

        public static void SetDefaultGroup(string groupId)
        {
            instance.defaultGroup = groupId;
            SaveConfig();
        }

        public struct MySqlSettings
        {
            public string Database;
            public string Username;
            public string Password;
            public string Address;
            public ushort Port;
        }
    }
}
