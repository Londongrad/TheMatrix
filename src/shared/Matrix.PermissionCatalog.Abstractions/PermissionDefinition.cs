namespace Matrix.PermissionCatalog.Abstractions
{
    public sealed record PermissionDefinition(
        string Key,
        string Service,
        string Group,
        string Description);
}
