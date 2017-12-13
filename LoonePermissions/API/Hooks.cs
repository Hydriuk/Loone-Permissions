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
        void OpenSteamBrowser(IRocketPlayer p, string url);

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

        public void OpenSteamBrowser(IRocketPlayer p, string url)
        {
            object obj = Convert.ChangeType(p, PlayerType);
            OpenURLInSteamOverlay.Invoke(obj, new object[] { "LoonePermissions requests that you open this link!", url });
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

    public class TLFProvider : IGameHook
    {
        public string DeterminingAssembly => "Rocket.TLF";

        MethodInfo SayToPlayer { get; set; }
        MethodInfo SayToServer { get; set; }
        MethodInfo OpenURLInSteamOverlay { get; set; }

        public void Initialize()
        {
            SayToPlayer = LoonePermissions.GameAssembly.GetType("ChubbyQuokka.Networking.Provider").GetMethod("MessagePlayer", new Type[] { typeof(ulong), typeof(string), typeof(Color) });
            SayToServer = LoonePermissions.RocketAssembly.GetType("Rocket.TLF.Chat").GetMethod("Broadcast", new Type[] { typeof(string), typeof(Color) });
            OpenURLInSteamOverlay = LoonePermissions.GameAssembly.GetType("ChubbyQuokka.Steam.SteamProvider").GetMethod("OpenInSteamBrowser", new Type[] { typeof(ulong), typeof(string) });
        }

        public void OpenSteamBrowser(IRocketPlayer p, string url)
        {
            OpenURLInSteamOverlay.Invoke(null, new object[] { ulong.Parse(p.Id), url });
        }

        public void Say(IRocketPlayer p, string text, Color c)
        {
            SayToPlayer.Invoke(null, new object[] { ulong.Parse(p.Id), text, c });
        }

        public void Say(string text, Color c)
        {
            SayToServer.Invoke(null, new object[] { text, c });
        }
    }
}
