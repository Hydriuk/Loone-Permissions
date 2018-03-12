using System;

using Rocket.API;
using RocketLogger = Rocket.Core.Logging.Logger;

using UnityEngine;

using LoonePermissions.Managers;
using LoonePermissions.API;

namespace LoonePermissions.Commands
{/*
    public class CommandGroup : ILooneCommand
    {
        public string Help => "Modifies a group!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 3)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            string groupId = args[0].ToLower();
            string modify = args[1].ToLower();
            string value = args[2];

            if (!MySqlManager.GroupExists(groupId))
            {
                LoonePermissions.Say(caller, "group_not_exists", Color.red);
                return;
            }

            switch (modify)
            {
                case "name":
                    MySqlManager.UpdateGroup(EGroupProperty.NAME, value, groupId);
                    LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                case "parent":
                    MySqlManager.UpdateGroup(EGroupProperty.PARENT, value, groupId);
                    LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                case "prefix":
                    MySqlManager.UpdateGroup(EGroupProperty.PREFIX, value, groupId);
                    LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                case "suffix":
                    MySqlManager.UpdateGroup(EGroupProperty.SUFFIX, value, groupId);
                    LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                case "color":
                    if (!MySqlManager.UpdateGroup(EGroupProperty.COLOR, value, groupId))
                        LoonePermissions.Say(caller, "invalid_color", Color.red);
                    else
                        LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                case "priority":
                    if (!MySqlManager.UpdateGroup(EGroupProperty.PRIORITY, value, groupId))
                        LoonePermissions.Say(caller, "invalid_num", Color.red);
                    else
                        LoonePermissions.Say(caller, "group_modified", Color.green, modify, groupId, value);
                    break;
                default:
                    LoonePermissions.Say(caller, "invalid_args", Color.red);
                    break;
            }

            RocketLogger.Log(string.Format("{0} has set the {1} of {2} to {3}!", caller.DisplayName, modify, groupId, value), ConsoleColor.Yellow);
        }
    }

    public class CommandDelete : ILooneCommand
    {
        public string Help => "Deletes a group and migrates every player to the default!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 1)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            if (!MySqlManager.GroupExists(args[0].ToLower()))
            {
                LoonePermissions.Say(caller, "group_not_exists", Color.red);
                return;
            }

            if (LoonePermissionsConfig.DefaultGroup.ToLower() == args[0])
            {
                LoonePermissions.Say(caller, "group_delete_default", Color.red);
                return;
            }

            MySqlManager.DeleteGroup(args[0].ToLower());
            LoonePermissions.Say(caller, "group_delete", Color.green, args[0].ToLower(), LoonePermissionsConfig.DefaultGroup.ToLower());
            RocketLogger.Log(string.Format("{0} deleted the group {1}!", caller.DisplayName, args[0].ToLower()), ConsoleColor.Yellow);
        }
    }

    public class CommandCreate : ILooneCommand
    {
        public string Help => "Creates a new group!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 1)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            if (MySqlManager.GroupExists(args[0].ToLower()))
            {
                LoonePermissions.Say(caller, "group_exists", Color.red);
                return;
            }

            MySqlManager.CreateGroup(args[0].ToLower());
            LoonePermissions.Say(caller, "group_create", Color.green, args[0].ToLower());
            RocketLogger.Log(string.Format("{0} created the group {1}!", caller.DisplayName, args[0].ToLower()), ConsoleColor.Yellow);
        }
    }

    public class CommandDefault : ILooneCommand
    {
        public string Help => "Sets a default group and migrates every player!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 1)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            if (!MySqlManager.GroupExists(args[0].ToLower()))
            {
                LoonePermissions.Say(caller, "group_not_exists", Color.red);
                return;
            }

            if (LoonePermissionsConfig.DefaultGroup.ToLower() == args[0].ToLower())
            {
                LoonePermissions.Say(caller, "group_default_already", Color.red);
                return;
            }

            MySqlManager.ReassignTo(LoonePermissionsConfig.DefaultGroup.ToLower(), args[0].ToLower(), true);
            LoonePermissionsConfig.SetDefaultGroup(args[0].ToLower());
            LoonePermissions.Say(caller, "group_default", Color.green, args[0].ToLower());
            RocketLogger.Log(string.Format("{0} set the default group to {1}!", caller.DisplayName, args[0].ToLower()), ConsoleColor.Yellow);
        }
    }

    public class CommandAdd : ILooneCommand
    {
        public string Help => "Sets a default group and migrates every player!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            if (!MySqlManager.GroupExists(args[0].ToLower()))
            {
                LoonePermissions.Say(caller, "group_not_exists", Color.red);
                return;
            }

            string groupId = args[0].ToLower();
            string perm = args[1].ToLower();

            bool doModify;
            int oldCooldown = MySqlManager.GetPermission(perm, groupId);

            doModify = oldCooldown != -1;

            int cooldown;

            if (args.Length == 3)
            {
                if (!int.TryParse(args[2], out cooldown))
                {
                    LoonePermissions.Say(caller, "invalid_args", Color.red);
                    return;
                }
            }
            else
            {
                cooldown = 0;
            }

            if (doModify)
            {
                if (oldCooldown == cooldown)
                {
                    LoonePermissions.Say(caller, "perm_exists", Color.red);
                    return;
                }
                else
                {
                    MySqlManager.ModifyPermsission(groupId, perm, cooldown);
                    LoonePermissions.Say(caller, "perm_modified", Color.green, perm, groupId, cooldown);
                    RocketLogger.Log(string.Format("{0} has set the cooldown of {1} to {2} in {3}!", caller.DisplayName, perm, cooldown, groupId), ConsoleColor.Yellow);
                }
            }
            else
            {
                MySqlManager.AddPermission(groupId, perm, cooldown);
                LoonePermissions.Say(caller, "perm_added", Color.green, perm, groupId, cooldown);
                RocketLogger.Log(string.Format("{0} added the permission {1} with a cooldown of {2} to {3}!", caller.DisplayName, perm, cooldown, groupId), ConsoleColor.Yellow);
            }
        }
    }

    public class CommandRemove : ILooneCommand
    {
        public string Help => "Sets a default group and migrates every player!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            if (args.Length != 2)
            {
                LoonePermissions.Say(caller, "invalid_args", Color.red);
                return;
            }

            string groupId = args[0].ToLower();
            string perm = args[1].ToLower();

            if (!MySqlManager.GroupExists(groupId))
            {
                LoonePermissions.Say(caller, "group_not_exists", Color.red);
                return;
            }

            if (MySqlManager.GetPermission(perm, groupId) == -1)
            {
                LoonePermissions.Say(caller, "perm_not_exists", Color.red, groupId, perm);
                return;
            }

            MySqlManager.RemovePermission(groupId, perm);
            LoonePermissions.Say(caller, "perm_removed", Color.green, perm, groupId);
            RocketLogger.Log(string.Format("{0} removed the permission {1} from {2}!", caller.DisplayName, perm, groupId), ConsoleColor.Yellow);
        }
    }

    public class CommandMigrate : ILooneCommand
    {
        public string Help => "Migrates all data from the XML file to MySQL!";

        public void Excecute(IRocketPlayer caller, string[] args)
        {
            LoonePermissions.Say(caller, "migrate_start", Color.green);
            try
            {
                MySqlManager.MigrateDatabase();
            }
            catch (Exception e)
            {
                LoonePermissions.Say(caller, "migrate_fail", Color.red);
                RocketLogger.LogException(e);
            }
            LoonePermissions.Say(caller, "migrate_finish", Color.green);
        }
    }
    */
}
