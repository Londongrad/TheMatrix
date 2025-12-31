namespace Matrix.Identity.Contracts.Admin.Users.Responses
{
    public sealed class UserPermissionResponse
    {
        public string PermissionKey { get; init; } = string.Empty;
        public string Effect { get; init; } = string.Empty;
    }
}
