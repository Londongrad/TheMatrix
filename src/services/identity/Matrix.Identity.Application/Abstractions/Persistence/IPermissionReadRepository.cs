using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IPermissionReadRepository
    {
        Task<IReadOnlyCollection<PermissionCatalogItemResult>> GetPermissionsAsync(CancellationToken ct);

        Task<PermissionCatalogItemResult?> GetPermissionAsync(
            string permissionKey,
            CancellationToken ct);
    }
}
