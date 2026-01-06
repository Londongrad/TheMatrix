namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        #region [ Identity permission keys ]

        public const string IdentityAdminAccess = "identity.admin.access";
        public const string IdentityUsersRead = "identity.admin.users.read";

        public const string IdentityUsersLock = "identity.admin.users.lock";
        public const string IdentityUsersUnlock = "identity.admin.users.unlock";

        public const string IdentityUserRolesRead = "identity.admin.users.roles.read";
        public const string IdentityUserRolesUpdate = "identity.admin.users.roles.update";

        public const string IdentityUserPermissionsRead = "identity.admin.users.permissions.read";
        public const string IdentityUserPermissionsGrant = "identity.admin.users.permissions.grant";
        public const string IdentityUserPermissionsDeprive = "identity.admin.users.permissions.deprive";

        public const string IdentityUserSessionsRead = "identity.admin.users.sessions.read";
        public const string IdentityUserSessionsRevoke = "identity.admin.users.sessions.revoke";
        public const string IdentityUserSessionsRevokeAll = "identity.admin.users.sessions.revoke.all";

        public const string IdentityRolesList = "identity.admin.roles.list";
        public const string IdentityRolesCreate = "identity.admin.roles.create";
        public const string IdentityRolesRename = "identity.admin.roles.rename";
        public const string IdentityRolesDelete = "identity.admin.roles.delete";
        public const string IdentityRolePermissionsRead = "identity.admin.roles.permissions.read";
        public const string IdentityRolePermissionsUpdate = "identity.admin.roles.permissions.update";
        public const string IdentityRoleMembersRead = "identity.admin.roles.members.read";
        public const string IdentityPermissionsCatalogRead = "identity.admin.permissions.catalog.read";

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
