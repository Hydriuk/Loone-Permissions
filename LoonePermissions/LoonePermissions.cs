using System;
using System.Reflection;
using System.Collections.Generic;

using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Plugins;
using RocketLogger = Rocket.Core.Logging.Logger;

using UnityEngine;

using ChubbyQuokka.LoonePermissions.Hooks;
using ChubbyQuokka.LoonePermissions.Managers;
using ChubbyQuokka.LoonePermissions.Providers;

namespace ChubbyQuokka.LoonePermissions
{
    public sealed class LoonePermissionsPlugin : RocketPlugin
    {
        public static LoonePermissionsPlugin Instance { get; private set; }
        internal static MySqlPermissionProvider Provider { get; private set; }
        internal static Assembly GameAssembly { get; private set; }
        internal static Assembly RocketAssembly { get; private set; }
        internal static IGameHook GameHook { get; private set; }

        static List<IGameHook> hooks = new List<IGameHook>();

        static IRocketPermissionsProvider RocketPermissionProvider;

        protected override void Load()
        {
            Instance = this;

            hooks.Add(new UnturnedProvider());

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++) {
                if (assemblies[i].GetName().Name == "Assembly-CSharp") {
                    GameAssembly = assemblies[i];
                    break;
                }
            }

            for (int i = 0; i < hooks.Count; i++) {
                for (int ii = 0; ii < assemblies.Length; ii++) {
                    if (hooks[i].DeterminingAssembly == assemblies[ii].GetName().Name) {
                        GameHook = hooks[i];
                        RocketAssembly = assemblies[ii];
                        goto FoundRocketAssembly;
                    }
                }
            }

            RocketLogger.Log("Failed to find any supporting games! Please only use this plugin with supported games.");
            UnloadPlugin();
            return;

        FoundRocketAssembly:

            RocketLogger.Log(string.Format("Welcome to Loone Permissions v{0}!", Assembly.GetName().Version), ConsoleColor.Yellow);
            RocketLogger.Log(string.Format("Plugin is now running in compatibility mode with: {0}", GameHook.DeterminingAssembly), ConsoleColor.Yellow);

            GameHook.Initialize();

            ThreadedWorkManager.Initialize();
            MySqlManager.Initialize();
            CommandManager.Initialize();

            Invoke("LateInit", 1f);
        }

        protected override void Unload()
        {
            if (RocketPermissionProvider != null)
                R.Permissions = RocketPermissionProvider;

            ThreadedWorkManager.Destroy();
            MySqlManager.Destroy();
        }

        void Update()
        {
            ThreadedWorkManager.Update();
        }

        void LateInit()
        {
            RocketPermissionProvider = R.Permissions;
            Provider = new MySqlPermissionProvider();
            R.Permissions = Provider;

            RocketLogger.Log(string.Format("Late Initialize was successful!"), ConsoleColor.Yellow);
        }

        public override TranslationList DefaultTranslations =>  new TranslationList
        {
            { "invalid_args", "You have specified an invalid arguement!" },
            { "invalid_perms", "You don't have permission to do this!" },
            { "invalid_cmd", "That commands doesn't exist!" },
            { "invalid_color", "You specified an invalid color!" },
            { "invalid_num", "Please specify an actual number!" },
            { "group_create", "You have created the group: {0}!" },
            { "group_delete", "You have deleted the group: {0}, all players in this group were moved to {1}!" },
            { "group_delete_default", "You can't delete the default group!" },
            { "group_default", "You have changed the default group to {0}!" },
            { "group_default_already", "This is already the default group!" },
            { "group_exists", "That group already exists!" },
            { "group_not_exists", "That group doesn't exist!" },
            { "group_modified", "You have set the {0} of {1} to {2}!" },
            { "perm_added", "The permission {0} has been added to {1} with a cooldown of {2}!" },
            { "perm_removed", "The permission {0} has been removed from {1}!" },
            { "perm_exists", "The group {0} already has the permission {1} with a cooldown of {2}! " },
            { "perm_not_exists", "The group {0} doesn't have the permission {1}!" },
            { "perm_modified", "The cooldown for {0} in the group {1} has been set to {2}!"},
            { "migrate_start", "The migration from XML to MySQL has started!"},
            { "migrate_finish", "The migration has finished!"},
            { "migrate_fail", "The migration failed!"}
        };

        internal static void Say(IRocketPlayer caller, string message, Color color, params object[] objs)
        {
            if (caller is ConsolePlayer)
            {
                RocketLogger.Log(Instance.Translate(message, objs), ConsoleColor.Yellow);
            }
            else
            {
                GameHook.Say(caller, Instance.Translate(message, objs), color);
            }
        }
    }

    
    public sealed class LoonePermissionsConfig : IRocketPluginConfiguration
    {
        public string DefaultGroup;

        public MySqlSettings DatabaseSettings;
        
        public void LoadDefaults()
        {
            DefaultGroup = "default";

            DatabaseSettings = new MySqlSettings
            {
                Database = "unturned",
                Username = "root",
                Password = "toor",
                Address = "127.0.0.1",
                Port = 3306,
                PlayerTableName = "loone_players",
                GroupsTableName = "loone_groups",
                PermissionsTableName = "loone_permissions"
            };
        }

        internal void SetDefaultGroup(string groupId)
        {
            DefaultGroup = groupId;
        }

        public struct MySqlSettings
        {
            public string Database;
            public string Username;
            public string Password;
            public string Address;
            public ushort Port;

            public string PlayerTableName;
            public string PermissionsTableName;
            public string GroupsTableName;
        }
    }
}
