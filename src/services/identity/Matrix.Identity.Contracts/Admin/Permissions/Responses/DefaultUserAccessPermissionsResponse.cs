namespace Matrix.Identity.Contracts.Admin.Permissions.Responses
{
    public sealed class DefaultUserAccessPermissionsResponse
    {
        public int Version { get; set; }
        public IReadOnlyCollection<string> PermissionKeys { get; set; } = Array.Empty<string>();
    }
}
