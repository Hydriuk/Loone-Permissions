using System;
using System.Linq;
using System.Collections.Generic;

using ChubbyQuokka.LoonePermissions.Managers;

using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Logging;

namespace ChubbyQuokka.LoonePermissions.Providers
{
    public class MySqlPermissionProvider : IRocketPermissionsProvider
    {
        public RocketPermissionsProviderResult AddGroup(RocketPermissionsGroup group) => PermissionManager.AddGroupBlocking(group);

        public RocketPermissionsProviderResult AddPlayerToGroup(string groupId, IRocketPlayer player) => PermissionManager.AddPlayerToGroupBlocking(groupId, player);

        public RocketPermissionsProviderResult DeleteGroup(string groupId) => PermissionManager.DeleteGroupBlocking(groupId);

        public RocketPermissionsGroup GetGroup(string groupId) => PermissionManager.GetGroupBlocking(groupId);

        public List<RocketPermissionsGroup> GetGroups(IRocketPlayer player, bool includeParentGroups) => PermissionManager.GetGroupsBlocking(player, includeParentGroups);

        public List<Permission> GetPermissions(IRocketPlayer player) => PermissionManager.GetPermissionsBlocking(player);

        public List<Permission> GetPermissions(IRocketPlayer player, List<string> requestedPermissions) => PermissionManager.GetPermissionsBlocking(player, requestedPermissions);

        public bool HasPermission(IRocketPlayer player, List<string> requestedPermissions) => PermissionManager.HasPermissionBlocking(player, requestedPermissions);

        public void Reload() => PermissionManager.Reload();

        public RocketPermissionsProviderResult RemovePlayerFromGroup(string groupId, IRocketPlayer player) => PermissionManager.RemovePlayerFromGroupBlocking(groupId, player);

        public RocketPermissionsProviderResult SaveGroup(RocketPermissionsGroup group) => PermissionManager.SaveGroupBlocking(group);
    }
}
