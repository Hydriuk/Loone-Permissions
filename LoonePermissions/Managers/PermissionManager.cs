using System;
using System.Collections.Generic;

using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Logging;
using Rocket.Core.Assets;

using MySql.Data.MySqlClient;

using ChubbyQuokka.LoonePermissions.API;

namespace ChubbyQuokka.LoonePermissions.Managers
{
    public static class PermissionManager
    {
        public static void Reload()
        {
            Logger.LogWarning("It is not necessary to reload permissions with this plugin!");
        }

        #region Blocking Calls
        public static RocketPermissionsProviderResult AddGroupBlocking(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult AddPlayerToGroupBlocking(string groupId, IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult DeleteGroupBlocking(string groupId)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsGroup GetGroupBlocking(string groupId)
        {
            throw new NotImplementedException();
        }

        public static List<RocketPermissionsGroup> GetGroupsBlocking(IRocketPlayer player, bool includeParentGroups)
        {
            throw new NotImplementedException();
        }

        public static List<Permission> GetPermissionsBlocking(IRocketPlayer player)
        {
            throw new NotImplementedException();
        } 

        public static List<Permission> GetPermissionsBlocking(IRocketPlayer player, List<string> requestedPermissions)
        {
            throw new NotImplementedException();
        }

        public static bool HasPermissionBlocking(IRocketPlayer player, List<string> requestedPermissions)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult RemovePlayerFromGroupBlocking(string groupId, IRocketPlayer player)
        {
            throw new NotImplementedException();
        }

        public static RocketPermissionsProviderResult SaveGroupBlocking(RocketPermissionsGroup group)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Async Calls

        public static void AddGroupAsync(RocketPermissionsGroup group, Action<RocketPermissionsProviderResult> callback)
        {
            throw new NotImplementedException();
        }

        public static void AddPlayerToGroupAsync(string groupId, IRocketPlayer player, Action<RocketPermissionsProviderResult> callback)
        {
            throw new NotImplementedException();
        }

        public static void DeleteGroupAsync(string groupId, Action<RocketPermissionsProviderResult> callback)
        {
            throw new NotImplementedException();
        }

        public static void GetGroupAsync(string groupId, Action<RocketPermissionsGroup> callback)
        {
            throw new NotImplementedException();
        }

        public static void GetGroupsAsync(IRocketPlayer player, bool includeParentGroups, Action<List<RocketPermissionsGroup>> callback)
        {
            throw new NotImplementedException();
        }

        public static void GetPermissionsAsync(IRocketPlayer player, Action<List<Permission>> callback)
        {
            throw new NotImplementedException();
        }

        public static void GetPermissionsAsync(IRocketPlayer player, List<string> requestedPermissions, Action<List<Permission>> callback)
        {
            throw new NotImplementedException();
        }

        public static void HasPermissionAsync(IRocketPlayer player, List<string> requestedPermissions, Action<bool> callback)
        {
            throw new NotImplementedException();
        }
   
        public static void RemovePlayerFromGroupAsync(string groupId, IRocketPlayer player, Action<RocketPermissionsProviderResult> callback)
        {
            throw new NotImplementedException();
        }

        public static void SaveGroupAsync(RocketPermissionsGroup group, Action<RocketPermissionsProviderResult> callback)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}