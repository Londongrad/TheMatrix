using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Catalog
{
    public interface IIdentityAdminCatalogClient
    {
        Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);
        Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(CancellationToken cancellationToken);
    }
}
