namespace Matrix.Identity.Contracts.Admin.Users.Requests
{
    public sealed class UserPermissionRequest
    {
        public required string PermissionKey { get; init; }
    }
}
