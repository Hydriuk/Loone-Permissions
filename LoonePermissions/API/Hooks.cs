using System;
using System.Reflection;

using Rocket.API;

using UnityEngine;

namespace LoonePermissions.Hooks
{
    public interface IGameHook
    {
        void Initialize();

        void Say(IRocketPlayer p, string text, Color c);
        void Say(string text, Color c);

        string DeterminingAssembly { get; }
    }

    public class UnturnedProvider : IGameHook
    {
        public string DeterminingAssembly => "Rocket.Unturned";

        MethodInfo SayToPlayer { get; set; }
        MethodInfo SayToServer { get; set; }
        MethodInfo OpenURLInSteamOverlay { get; set; }

        Type PlayerType { get; set; }

        public void Initialize()
        {
            SayToPlayer = LoonePermissions.RocketAssembly.GetType("Rocket.Unturned.Chat.UnturnedChat").GetMethod("Say", new Type[] { typeof(IRocketPlayer), typeof(string), typeof(Color) });
            SayToServer = LoonePermissions.RocketAssembly.GetType("Rocket.Unturned.Chat.UnturnedChat").GetMethod("Say", new Type[] { typeof(string), typeof(Color) });
            PlayerType = LoonePermissions.RocketAssembly.GetType("Rocket.Unturned.Player.UnturnedPlayer");
            OpenURLInSteamOverlay = PlayerType.GetMethod("Player.sendBrowserRequest", new Type[] { typeof(string), typeof(string) });
        }

        public void Say(IRocketPlayer p, string text, Color c)
        {
            SayToPlayer.Invoke(null, new object[] { p, text, c });
        }

        public void Say(string text, Color c)
        {
            SayToServer.Invoke(null, new object[] { text, c });
        }
    }
}
