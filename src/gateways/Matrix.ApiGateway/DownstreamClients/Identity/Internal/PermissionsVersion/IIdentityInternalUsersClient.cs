using Matrix.Identity.Contracts.Internal.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion
{
    public interface IIdentityInternalUsersClient
    {
        Task<int> GetPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<UserAuthContextResponse> GetAuthContextAsync(
            Guid userId,
            CancellationToken cancellationToken);
    }
}
