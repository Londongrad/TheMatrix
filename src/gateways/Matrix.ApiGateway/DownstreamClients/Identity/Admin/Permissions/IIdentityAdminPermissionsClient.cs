using Matrix.Identity.Contracts.Admin.Permissions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions
{
    public interface IIdentityAdminPermissionsClient
    {
        Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(
            CancellationToken cancellationToken);
    }
}
