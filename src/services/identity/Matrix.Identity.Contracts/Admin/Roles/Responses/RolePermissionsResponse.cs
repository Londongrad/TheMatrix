namespace Matrix.Identity.Contracts.Admin.Roles.Responses
{
    public sealed class RolePermissionsResponse
    {
        public IReadOnlyCollection<string> PermissionKeys { get; init; } = Array.Empty<string>();
    }
}
