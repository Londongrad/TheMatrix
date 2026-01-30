using ContractPermissionKeys = Matrix.Identity.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.Identity.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string IdentityAdminAccess = ContractPermissionKeys.IdentityAdminAccess;
        public const string IdentityUsersRead = ContractPermissionKeys.IdentityUsersRead;

        public const string IdentityUsersLock = ContractPermissionKeys.IdentityUsersLock;
        public const string IdentityUsersUnlock = ContractPermissionKeys.IdentityUsersUnlock;

        public const string IdentityUserRolesRead = ContractPermissionKeys.IdentityUserRolesRead;
        public const string IdentityUserRolesUpdate = ContractPermissionKeys.IdentityUserRolesUpdate;

        public const string IdentityUserPermissionsRead = ContractPermissionKeys.IdentityUserPermissionsRead;
        public const string IdentityUserPermissionsGrant = ContractPermissionKeys.IdentityUserPermissionsGrant;
        public const string IdentityUserPermissionsDeprive = ContractPermissionKeys.IdentityUserPermissionsDeprive;

        public const string IdentityUserSessionsRead = ContractPermissionKeys.IdentityUserSessionsRead;
        public const string IdentityUserSessionsRevoke = ContractPermissionKeys.IdentityUserSessionsRevoke;
        public const string IdentityUserSessionsRevokeAll = ContractPermissionKeys.IdentityUserSessionsRevokeAll;

        public const string IdentityRolesList = ContractPermissionKeys.IdentityRolesList;
        public const string IdentityRolesCreate = ContractPermissionKeys.IdentityRolesCreate;
        public const string IdentityRolesRename = ContractPermissionKeys.IdentityRolesRename;
        public const string IdentityRolesDelete = ContractPermissionKeys.IdentityRolesDelete;
        public const string IdentityRolePermissionsRead = ContractPermissionKeys.IdentityRolePermissionsRead;
        public const string IdentityRolePermissionsUpdate = ContractPermissionKeys.IdentityRolePermissionsUpdate;
        public const string IdentityRoleMembersRead = ContractPermissionKeys.IdentityRoleMembersRead;
        public const string IdentityPermissionsCatalogRead = ContractPermissionKeys.IdentityPermissionsCatalogRead;

        public const string IdentityMeProfileRead = ContractPermissionKeys.IdentityMeProfileRead;
        public const string IdentityMePasswordChange = ContractPermissionKeys.IdentityMePasswordChange;
        public const string IdentityMeAvatarChange = ContractPermissionKeys.IdentityMeAvatarChange;

        public const string IdentityMeSessionsRead = ContractPermissionKeys.IdentityMeSessionsRead;
        public const string IdentityMeSessionsRevoke = ContractPermissionKeys.IdentityMeSessionsRevoke;
        public const string IdentityMeSessionsRevokeAll = ContractPermissionKeys.IdentityMeSessionsRevokeAll;
    }
}
