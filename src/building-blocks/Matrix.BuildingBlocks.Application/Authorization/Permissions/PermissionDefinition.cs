namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public sealed record PermissionDefinition(
        string Key,
        string Service,
        string Group,
        string Description);
}
