namespace Matrix.Identity.Contracts.Admin.Users.Requests
{
    public sealed class UpdateUserPermissionsRequest
    {
        public required IReadOnlyCollection<UserPermissionOverrideRequest> Overrides { get; init; }
    }
}
