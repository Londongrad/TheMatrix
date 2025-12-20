namespace Matrix.Identity.Application.Abstractions.Services.Authorization
{
    public sealed record AuthorizationContext(
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions,
        int PermissionsVersion);
}
