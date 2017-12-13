using System;
using System.Collections.Generic;

using LoonePermissions.API;
using LoonePermissions.Commands;
using LoonePermissions.Managers;

using Rocket.API;
using Rocket.Core;
using Rocket.Unturned.Player;
//using Rocket.Unturned.Player;

using UnityEngine;

namespace LoonePermissions.Managers
{
    public static class CommandManager
    {
        public static Dictionary<string, ILooneCommand> commands;

        public static void Initialize()
        {
            commands = new Dictionary<string, ILooneCommand>();

            RegisterCommand("create", new CommandCreate());
            RegisterCommand("delete", new CommandDelete());
            RegisterCommand("add", new CommandAdd());
            RegisterCommand("remove", new CommandRemove());
            RegisterCommand("default", new CommandRemove());
            RegisterCommand("group", new CommandGroup());
            RegisterCommand("migrate", new CommandMigrate());
        }

        public static void Excecute(IRocketPlayer caller, string cmd, string[] args)
        {

            if (!TryGetCommand(cmd, out ILooneCommand command))
            {
                LoonePermissions.Say(caller, "invalid_cmd", Color.red);
                return;
            }

            if (!HasPermission(caller, cmd))
            {
                LoonePermissions.Say(caller, "invalid_perms", Color.red);
                return;
            }

            command.Excecute(caller, args);
        }

        public static bool HasPermission(IRocketPlayer caller, string cmd)
        {
            return R.Permissions.HasPermission(caller, new List<string> { "loone." + cmd });
        }

        public static bool RegisterCommand(string str, ILooneCommand cmd)
        {
            if (commands.ContainsKey(str))
                return false;

            commands.Add(str, cmd);
            return true;
        }

        public static bool TryGetCommand(string str, out ILooneCommand cmd)
        {
            return commands.TryGetValue(str, out cmd);
        }

    }
}

namespace LoonePermissions
{
    public class LooneCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "loone";

        public string Help => "The universal LoonePermissions command!";

        public string Syntax => "<command> <args>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
                if (!(caller is ConsolePlayer))
                    ((UnturnedPlayer)caller).Player.sendBrowserRequest("Here's a link to the Loone Permissions wiki!", "https://github.com/ChubbyQuokka/Loone-Permissions/wiki");
                else
                    LoonePermissions.Say(caller, "https://github.com/ChubbyQuokka/Loone-Permissions/wiki", Color.black);

            string[] args = new string[command.Length - 1];

            for (int i = 1; i < command.Length; i++)
                args[i - 1] = command[i];

            CommandManager.Excecute(caller, command[0], args);
        }
    }
}
