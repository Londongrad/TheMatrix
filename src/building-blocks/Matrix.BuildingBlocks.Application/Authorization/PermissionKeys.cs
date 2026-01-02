namespace Matrix.BuildingBlocks.Application.Authorization
{
    public static class PermissionKeys
    {
        #region [ Identity permission keys ]

        public const string IdentityAdminAccess = "identity.admin.access";
        public const string IdentityUsersRead = "identity.users.read";

        public const string IdentityUsersLock = "identity.users.lock";
        public const string IdentityUsersUnlock = "identity.users.unlock";

        public const string IdentityUserRolesRead = "identity.users.roles.read";
        public const string IdentityUserRolesUpdate = "identity.users.roles.update";

        public const string IdentityUserPermissionsRead = "identity.users.permissions.read";
        public const string IdentityUserPermissionsGrant = "identity.users.permissions.grant";
        public const string IdentityUserPermissionsDeprive = "identity.users.permissions.deprive";

        public const string IdentityUserSessionsRead = "identity.users.sessions.read";
        public const string IdentityUserSessionsRevoke = "identity.users.sessions.revoke";
        public const string IdentityUserSessionsRevokeAll = "identity.users.sessions.revoke.all";

        public const string IdentityRolesList = "identity.roles.list";
        public const string IdentityRolesCreate = "identity.roles.create";
        public const string IdentityRolesRename = "identity.roles.rename";
        public const string IdentityRolesDelete = "identity.roles.delete";
        public const string IdentityRolePermissionsRead = "identity.roles.permissions.read";
        public const string IdentityRolePermissionsUpdate = "identity.roles.permissions.update";
        public const string IdentityRoleMembersRead = "identity.roles.members.read";
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

        public const string PopulationPersonResurrect = "population.person.resurrect";
        public const string PopulationPersonKill = "population.person.kill";

        #endregion [ Population permission keys ]
    }
}
