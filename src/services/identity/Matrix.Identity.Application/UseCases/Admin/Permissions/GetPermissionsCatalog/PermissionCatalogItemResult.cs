namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog
{
    public sealed class PermissionCatalogItemResult
    {
        public string Key { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool IsDeprecated { get; init; }
    }
}
