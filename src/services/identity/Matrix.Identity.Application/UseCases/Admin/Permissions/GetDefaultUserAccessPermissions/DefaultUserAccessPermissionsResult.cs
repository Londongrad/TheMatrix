namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions
{
    public sealed record DefaultUserAccessPermissionsResult(
        int Version,
        IReadOnlyCollection<string> PermissionKeys);
}
