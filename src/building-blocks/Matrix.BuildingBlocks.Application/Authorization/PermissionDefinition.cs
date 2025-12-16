namespace Matrix.BuildingBlocks.Application.Authorization
{
    public sealed record PermissionDefinition(
        string Key,
        string Service,
        string Group,
        string Description);
}
