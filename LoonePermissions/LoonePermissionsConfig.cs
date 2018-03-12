using System.Xml.Serialization;

using Rocket.API;

namespace ChubbyQuokka.LoonePermissions
{
    public sealed class LoonePermissionsConfig : IRocketPluginConfiguration
    {
        public string defaultGroup;
        public string DefaultGroup => LoonePermissionsPlugin.Instance.Configuration.Instance.defaultGroup;

        public _DatabaseSettings databaseSettings;
        public _DatabaseSettings DatabaseSettings => LoonePermissionsPlugin.Instance.Configuration.Instance.databaseSettings;

        public _SyncModeSettings syncModeSettings;
        public _SyncModeSettings SyncModeSettings => LoonePermissionsPlugin.Instance.Configuration.Instance.syncModeSettings;

        public _AdvancedSettings advancedSettings;
        public _AdvancedSettings AdvancedSettings => LoonePermissionsPlugin.Instance.Configuration.Instance.advancedSettings;

        public void LoadDefaults()
        {
            defaultGroup = "default";

            databaseSettings = new _DatabaseSettings
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

            syncModeSettings = new _SyncModeSettings
            {
                Enabled = false,
                SyncTime = 10000
            };

            advancedSettings = new _AdvancedSettings
            {
                UseAsyncCommands = false,
                WorkerThreadSleepTime = 100
            };
        }

        [XmlRoot(ElementName = "DatabaseSettings")]
        public struct _DatabaseSettings
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

        [XmlRoot(ElementName = "SyncModeSettings")]
        public struct _SyncModeSettings
        {
            public bool Enabled;
            public uint SyncTime;
        }

        [XmlRoot(ElementName = "AdvancedSettings")]
        public struct _AdvancedSettings
        {
            public bool UseAsyncCommands;
            public ushort WorkerThreadSleepTime;
        }
    }
}
