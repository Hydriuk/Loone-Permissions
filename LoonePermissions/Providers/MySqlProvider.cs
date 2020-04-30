using System;
using System.Linq;
using System.Collections.Generic;

using LoonePermissions.Managers;

using Rocket.API;
using Rocket.API.Serialisation;

namespace LoonePermissions.Providers
{
    public class MySqlProvider : IRocketPermissionsProvider
    {

        public RocketPermissionsProviderResult AddPlayerToGroup(string groupId, IRocketPlayer player)
        {
            return MySqlManager.AddPlayerToGroup(ulong.Parse(player.Id), groupId);
        }

        public RocketPermissionsProviderResult RemovePlayerFromGroup(string groupId, IRocketPlayer player)
        {
            return MySqlManager.RemovePlayerFromGroup(ulong.Parse(player.Id), groupId);
        }

        public RocketPermissionsGroup GetGroup(string groupId)
        {
            return MySqlManager.GetGroup(groupId);
        }

        public List<RocketPermissionsGroup> GetGroups(IRocketPlayer player, bool includeParentGroups)
        {
            string[] groupId = MySqlManager.GetPlayerGroups(ulong.Parse(player.Id), true);
            List<RocketPermissionsGroup> groups = new List<RocketPermissionsGroup>();

            foreach (string str in groupId)
            {
                groups.Add(MySqlManager.GetGroup(str));
                groups.Add(MySqlManager.GetParentGroup(str));
            }

            return (from x in groups.Distinct()
                    orderby x.Priority
                    select x).ToList();
        }


        public List<Permission> GetPermissions(IRocketPlayer player)
        {
            List<RocketPermissionsGroup> groupId = GetGroups(player, true);

            List<Permission> perms = new List<Permission>();

            foreach (RocketPermissionsGroup str in groupId)
                perms.AddRange(MySqlManager.GetPermissionsByGroup(str.Id, false));

            return perms;
        }


        public List<Permission> GetPermissions(IRocketPlayer player, List<string> requestedPermissions)
        {
            return (from p in GetPermissions(player)
                    where requestedPermissions.Exists((string i) => string.Equals(i, p.Name, StringComparison.OrdinalIgnoreCase))
                    select p).ToList();
        }

        public bool HasPermission(IRocketPlayer player, List<string> requestedPermissions)
        {
            if (player.IsAdmin)
                return true;

            List<Permission> perms = GetPermissions(player);
            List<string> permsAsStrs = new List<string>();

            foreach (Permission perm in perms)
                permsAsStrs.Add(perm.Name);

            if (permsAsStrs.Contains("*"))
            {
                return true;
            }

            foreach (string str in requestedPermissions)
            {
                if (permsAsStrs.Contains(str) && !permsAsStrs.Contains("~" + str))
                    return true;
            }

            return false;
        }

        public void Reload()
        {

        }

        public RocketPermissionsProviderResult AddGroup(RocketPermissionsGroup group)
        {
            return RocketPermissionsProviderResult.UnspecifiedError;
        }

        public RocketPermissionsProviderResult SaveGroup(RocketPermissionsGroup group)
        {
            return RocketPermissionsProviderResult.UnspecifiedError;
        }

        public RocketPermissionsProviderResult DeleteGroup(string groupId)
        {
            return RocketPermissionsProviderResult.UnspecifiedError;
        }
    }
}
