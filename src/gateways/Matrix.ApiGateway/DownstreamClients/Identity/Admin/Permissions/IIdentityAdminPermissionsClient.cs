using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions
{
    public interface IIdentityAdminPermissionsClient
    {
        Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(
            CancellationToken cancellationToken);

        Task<DefaultUserAccessPermissionsResponse> GetDefaultUserAccessPermissionsAsync(
            CancellationToken cancellationToken);

        Task UpdateDefaultUserAccessPermissionsAsync(
            UpdateDefaultUserAccessPermissionsRequest request,
            CancellationToken cancellationToken);
    }
}
