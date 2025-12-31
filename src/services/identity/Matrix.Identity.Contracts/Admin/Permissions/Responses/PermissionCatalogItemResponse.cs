namespace Matrix.Identity.Contracts.Admin.Permissions.Responses
{
    public sealed class PermissionCatalogItemResponse
    {
        public string Key { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool IsDeprecated { get; init; }
    }
}
