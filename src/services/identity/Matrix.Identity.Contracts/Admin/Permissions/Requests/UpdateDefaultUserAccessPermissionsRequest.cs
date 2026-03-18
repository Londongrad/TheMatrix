namespace Matrix.Identity.Contracts.Admin.Permissions.Requests
{
    public sealed class UpdateDefaultUserAccessPermissionsRequest
    {
        public IReadOnlyCollection<string> PermissionKeys { get; set; } = Array.Empty<string>();
    }
}
