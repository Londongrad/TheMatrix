namespace Matrix.Identity.Contracts.Admin.Users.Requests
{
    public sealed class UserPermissionOverrideRequest
    {
        public required string PermissionKey { get; init; }
        public required string Effect { get; init; }
    }
}
