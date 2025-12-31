namespace Matrix.Identity.Contracts.Admin.Roles.Requests
{
    public sealed class UpdateRolePermissionsRequest
    {
        public IReadOnlyCollection<string> PermissionKeys { get; init; } = Array.Empty<string>();
    }
}
