using System;
using System.Linq;
using System.Collections.Generic;

using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Logging;
using ChubbyQuokka.LoonePermissions.Extensions;

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
            return MySqlManager.AddGroup(group);
        }

        public static RocketPermissionsProviderResult DeleteGroupBlocking(string groupId)
        {
            return MySqlManager.DeleteGroup(groupId);
        }

        public static RocketPermissionsProviderResult SaveGroupBlocking(RocketPermissionsGroup group)
        {
            return MySqlManager.SaveGroup(group);
        }

        //READY
        public static RocketPermissionsProviderResult AddPlayerToGroupBlocking(string groupId, IRocketPlayer player)
        {
            return MySqlManager.AddPlayerToGroup(groupId, player);
        }

        //READY
        public static RocketPermissionsProviderResult RemovePlayerFromGroupBlocking(string groupId, IRocketPlayer player)
        {
            return MySqlManager.RemovePlayerFromGroup(groupId, player);
        }
        
        //READY
        public static RocketPermissionsGroup GetGroupBlocking(string groupId)
        {
            return MySqlManager.GetGroup(groupId);
        }

        //READY
        public static List<RocketPermissionsGroup> GetGroupsBlocking(IRocketPlayer player, bool includeParentGroups)
        {
            return MySqlManager.GetGroups(player, includeParentGroups);
        }

        //READY
        public static List<Permission> GetPermissionsBlocking(IRocketPlayer player)
        {
            List<RocketPermissionsGroup> groups = MySqlManager.GetGroups(player, true) ?? new List<RocketPermissionsGroup>();
            groups.Reverse();

            List<Permission> perms = new List<Permission>();

            foreach (RocketPermissionsGroup g in groups)
            {
                if (g != null)
                {
                    foreach (Permission p in g.Permissions)
                    {
                        if (p != null)
                        {
                            if (p.Name.StartsWith("-"))
                            {
                                perms.RemoveAll(x => string.Equals(x.Name, p.Name.Replace("-", ""), StringComparison.InvariantCultureIgnoreCase));
                            }
                            else
                            {
                                perms.RemoveAll(x => x.Name.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase));
                                perms.Add(p);
                            }
                        }
                    }
                }
            }

            return perms;
        }

        //READY
        public static List<Permission> GetPermissionsBlocking(IRocketPlayer player, List<string> requestedPermissions)
        {
            List<string> newPerms = requestedPermissions?.Where(x => !string.IsNullOrEmpty(x))?.Select(x => x)?.Distinct()?.ToList() ?? new List<string>();

            return GetPermissionsBlocking(player)?.Where(x => newPerms.ContainsIgnoreCase(x.Name))?.ToList() ?? new List<Permission>();
        }

        //READY
        public static bool HasPermissionBlocking(IRocketPlayer player, List<string> requestedPermissions)
        {
            if (player.IsAdmin)
            {
                return true;
            }

            return GetPermissionsBlocking(player, requestedPermissions).Count != 0;
        }

        #endregion
        #region Async Calls

        public static void AddGroupAsync(RocketPermissionsGroup group, Action<RocketPermissionsProviderResult> callback)
        {
            Action async = () =>
            {
                RocketPermissionsProviderResult result = AddGroupBlocking(group);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void AddPlayerToGroupAsync(string groupId, IRocketPlayer player, Action<RocketPermissionsProviderResult> callback)
        {
            Action async = () =>
            {
                RocketPermissionsProviderResult result = AddPlayerToGroupBlocking(groupId, player);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void DeleteGroupAsync(string groupId, Action<RocketPermissionsProviderResult> callback)
        {
            Action async = () =>
            {
                RocketPermissionsProviderResult result = DeleteGroupBlocking(groupId);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void GetGroupAsync(string groupId, Action<RocketPermissionsGroup> callback)
        {
            Action async = () =>
            {
                RocketPermissionsGroup result = GetGroupBlocking(groupId);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void GetGroupsAsync(IRocketPlayer player, bool includeParentGroups, Action<List<RocketPermissionsGroup>> callback)
        {
            Action async = () =>
            {
                List<RocketPermissionsGroup> result = GetGroupsBlocking(player, includeParentGroups);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void GetPermissionsAsync(IRocketPlayer player, Action<List<Permission>> callback)
        {
            Action async = () =>
            {
                List<Permission> result = GetPermissionsBlocking(player);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void GetPermissionsAsync(IRocketPlayer player, List<string> requestedPermissions, Action<List<Permission>> callback)
        {
            Action async = () =>
            {
                List<Permission> result = GetPermissionsBlocking(player, requestedPermissions);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void HasPermissionAsync(IRocketPlayer player, List<string> requestedPermissions, Action<bool> callback)
        {
            Action async = () =>
            {
                bool result = HasPermissionBlocking(player, requestedPermissions);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }
   
        public static void RemovePlayerFromGroupAsync(string groupId, IRocketPlayer player, Action<RocketPermissionsProviderResult> callback)
        {
            Action async = () =>
            {
                RocketPermissionsProviderResult result = RemovePlayerFromGroupBlocking(groupId, player);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }

        public static void SaveGroupAsync(RocketPermissionsGroup group, Action<RocketPermissionsProviderResult> callback)
        {
            Action async = () =>
            {
                RocketPermissionsProviderResult result = SaveGroupBlocking(group);

                Action _callback = () =>
                {
                    callback.Invoke(result);
                };

                ThreadedWorkManager.EnqueueMainThread(_callback);
            };

            ThreadedWorkManager.EnqueueWorkerThread(async);
        }
#endregion
    }
}