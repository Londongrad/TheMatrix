namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";
        private const string IdentityService = "Identity";

        private const string AdminAccessGroup = "Admin / Access";
        private const string AdminUsersGroup = "Admin / Users";
        private const string AdminRolesGroup = "Admin / Roles";
        private const string AdminPermissionsGroup = "Admin / Permissions";
        private const string MeGroup = "Me";

        private const string PeopleGroup = "People";
        private const string PersonGroup = "Person";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            #region [ Identity permissions ]

            new(
                Key: PermissionKeys.IdentityAdminAccess,
                Service: IdentityService,
                Group: AdminAccessGroup,
                Description: "Access admin management endpoints."),
            new(
                Key: PermissionKeys.IdentityUsersRead,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "View user list and details."),
            new(
                Key: PermissionKeys.IdentityUsersLock,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Lock a user (disable login)."),
            new(
                Key: PermissionKeys.IdentityUsersUnlock,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Unlock a user (enable login)."),
            new(
                Key: PermissionKeys.IdentityUserRolesRead,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "View roles assigned to a user."),
            new(
                Key: PermissionKeys.IdentityUserRolesUpdate,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Assign/unassign roles for a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsRead,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "View direct permissions assigned to a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsGrant,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Grant direct permissions for a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsDeprive,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Deprive a user of a direct permission."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRead,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "View user sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRevoke,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Revoke user sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRevokeAll,
                Service: IdentityService,
                Group: AdminUsersGroup,
                Description: "Revoke all user sessions (all refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityRolesList,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "View roles list (paged)."),
            new(
                Key: PermissionKeys.IdentityRolesCreate,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "Create custom roles."),
            new(
                Key: PermissionKeys.IdentityRolesRename,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "Rename custom roles."),
            new(
                Key: PermissionKeys.IdentityRolesDelete,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "Delete custom roles."),
            new(
                Key: PermissionKeys.IdentityRolePermissionsRead,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "View permissions assigned to a role."),
            new(
                Key: PermissionKeys.IdentityRolePermissionsUpdate,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "Replace permissions assigned to a role."),
            new(
                Key: PermissionKeys.IdentityRoleMembersRead,
                Service: IdentityService,
                Group: AdminRolesGroup,
                Description: "View users assigned to a role (paged)."),
            new(
                Key: PermissionKeys.IdentityPermissionsCatalogRead,
                Service: IdentityService,
                Group: AdminPermissionsGroup,
                Description: "View permissions catalog."),
            new(
                Key: PermissionKeys.IdentityMeProfileRead,
                Service: IdentityService,
                Group: MeGroup,
                Description: "View own profile."),
            new(
                Key: PermissionKeys.IdentityMePasswordChange,
                Service: IdentityService,
                Group: MeGroup,
                Description: "Change own password."),
            new(
                Key: PermissionKeys.IdentityMeAvatarChange,
                Service: IdentityService,
                Group: MeGroup,
                Description: "Change own avatar."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRead,
                Service: IdentityService,
                Group: MeGroup,
                Description: "View own sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRevoke,
                Service: IdentityService,
                Group: MeGroup,
                Description: "Revoke one of own sessions (refresh token)."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRevokeAll,
                Service: IdentityService,
                Group: MeGroup,
                Description: "Revoke all own sessions (all refresh tokens)."),

            #endregion [ Identity permissions ]

            #region [ Population permissions ]

            new(
                Key: PermissionKeys.PopulationPeopleRead,
                Service: PopulationService,
                Group: PeopleGroup,
                Description: "Read citizens page."),
            new(
                Key: PermissionKeys.PopulationPeopleInitialize,
                Service: PopulationService,
                Group: PeopleGroup,
                Description: "Initialize population."),
            new(
                Key: PermissionKeys.PopulationPersonKill,
                Service: PopulationService,
                Group: PersonGroup,
                Description: "Kill a person."),
            new(
                Key: PermissionKeys.PopulationPersonResurrect,
                Service: PopulationService,
                Group: PersonGroup,
                Description: "Resurrect a person."),

            #endregion [ Population permissions ]
        ];

        public static IReadOnlyCollection<string> AllKeys =>
            All.Select(x => x.Key)
               .ToArray();

        public static PermissionDefinition GetOrThrow(string key)
        {
            PermissionDefinition? item = All.FirstOrDefault(x => x.Key == key);
            return item ?? throw new InvalidOperationException($"Permission not found in catalog: {key}");
        }
    }
}
