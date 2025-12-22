namespace Matrix.BuildingBlocks.Application.Authorization
{
    public static class PermissionKeys
    {
        #region [ Identity permission keys ]

        public const string IdentityUsersList = "identity.users.list";
        public const string IdentityUsersRead = "identity.users.read";

        public const string IdentityUsersLock = "identity.users.lock";
        public const string IdentityUsersUnlock = "identity.users.unlock";

        public const string IdentityUserRolesRead = "identity.users.roles.read";
        public const string IdentityUserRolesAssign = "identity.users.roles.assign";

        public const string IdentityUserPermissionsRead = "identity.users.permissions.read";
        public const string IdentityUserPermissionsGrant = "identity.users.permissions.grant";
        public const string IdentityUserPermissionsDeprive = "identity.users.permissions.deprive";

        public const string IdentityUserSessionsRead = "identity.users.sessions.read";
        public const string IdentityUserSessionsRevoke = "identity.users.sessions.revoke";
        public const string IdentityUserSessionsRevokeAll = "identity.users.sessions.revoke.all";

        public const string IdentityRolesList = "identity.roles.list";
        public const string IdentityRolesManage = "identity.roles.manage";
        public const string IdentityPermissionsCatalogRead = "identity.permissions.catalog.read";

        public const string IdentityMeProfileRead = "identity.me.profile.read";
        public const string IdentityMePasswordChange = "identity.me.password.change";
        public const string IdentityMeAvatarChange = "identity.me.avatar.change";

        public const string IdentityMeSessionsRead = "identity.me.sessions.read";
        public const string IdentityMeSessionsRevoke = "identity.me.sessions.revoke";
        public const string IdentityMeSessionsRevokeAll = "identity.me.sessions.revoke.all";

        #endregion [ Identity permission keys ]

        #region [ Population permission keys ]

        public const string PopulationPeopleInitialize = "population.people.initialize";
        public const string PopulationPeopleRead = "population.people.read";
        public const string PopulationPeopleKill = "population.people.kill";
        public const string PopulationPeopleResurrect = "population.people.resurrect";

        #endregion [ Population permission keys ]
    }
}
