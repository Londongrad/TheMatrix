using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IPermissionReadRepository
    {
        Task<IReadOnlyCollection<PermissionCatalogItemResult>> GetPermissionsAsync(CancellationToken cancellationToken);

        Task<PermissionCatalogItemResult?> GetPermissionAsync(
            string permissionKey,
            CancellationToken cancellationToken);
    }
}
