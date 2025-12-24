namespace Matrix.BuildingBlocks.Application.Authorization
{
    public static class PermissionsCatalog
    {
        private const string PopulationService = "Population";
        private const string IdentityService = "Identity";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            #region [ Identity permissions ]

            new(
                Key: PermissionKeys.IdentityUsersList,
                Service: IdentityService,
                Group: "Users",
                Description: "View user list (paged)."),
            new(
                Key: PermissionKeys.IdentityUsersRead,
                Service: IdentityService,
                Group: "Users",
                Description: "View user details."),
            new(
                Key: PermissionKeys.IdentityUsersLock,
                Service: IdentityService,
                Group: "Users",
                Description: "Lock a user (disable login)."),
            new(
                Key: PermissionKeys.IdentityUsersUnlock,
                Service: IdentityService,
                Group: "Users",
                Description: "Unlock a user (enable login)."),
            new(
                Key: PermissionKeys.IdentityUserRolesRead,
                Service: IdentityService,
                Group: "User Roles",
                Description: "View roles assigned to a user."),
            new(
                Key: PermissionKeys.IdentityUserRolesAssign,
                Service: IdentityService,
                Group: "User Roles",
                Description: "Assign/unassign roles for a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsRead,
                Service: IdentityService,
                Group: "User Permissions",
                Description: "View direct permissions assigned to a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsGrant,
                Service: IdentityService,
                Group: "User Permissions",
                Description: "Grant direct permissions for a user."),
            new(
                Key: PermissionKeys.IdentityUserPermissionsDeprive,
                Service: IdentityService,
                Group: "User Permissions",
                Description: "Deprive a user of a direct permission."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRead,
                Service: IdentityService,
                Group: "User Sessions",
                Description: "View user sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRevoke,
                Service: IdentityService,
                Group: "User Sessions",
                Description: "Revoke user sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityUserSessionsRevokeAll,
                Service: IdentityService,
                Group: "User Sessions",
                Description: "Revoke all user sessions (all refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityMeProfileRead,
                Service: IdentityService,
                Group: "Me",
                Description: "View own profile."),
            new(
                Key: PermissionKeys.IdentityMePasswordChange,
                Service: IdentityService,
                Group: "Me",
                Description: "Change own password."),
            new(
                Key: PermissionKeys.IdentityMeAvatarChange,
                Service: IdentityService,
                Group: "Me",
                Description: "Change own avatar."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRead,
                Service: IdentityService,
                Group: "Me Sessions",
                Description: "View own sessions (refresh tokens)."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRevoke,
                Service: IdentityService,
                Group: "Me Sessions",
                Description: "Revoke one of own sessions (refresh token)."),
            new(
                Key: PermissionKeys.IdentityMeSessionsRevokeAll,
                Service: IdentityService,
                Group: "Me Sessions",
                Description: "Revoke all own sessions (all refresh tokens)."),

            #endregion [ Identity permissions ]

            #region [ Population permissions ]

            new(
                Key: PermissionKeys.PopulationPeopleRead,
                Service: PopulationService,
                Group: "People",
                Description: "Read citizens page."),
            new(
                Key: PermissionKeys.PopulationPeopleInitialize,
                Service: PopulationService,
                Group: "People",
                Description: "Initialize population."),
            new(
                Key: PermissionKeys.PopulationPeopleKill,
                Service: PopulationService,
                Group: "People",
                Description: "Kill a person."),
            new(
                Key: PermissionKeys.PopulationPeopleResurrect,
                Service: PopulationService,
                Group: "People",
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
