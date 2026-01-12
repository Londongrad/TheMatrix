using Matrix.Identity.Contracts.Internal.Responses;

namespace Matrix.ApiGateway.Authorization.AuthContext.Abstractions
{
    public interface IAuthContextStore
    {
        Task<UserAuthContextResponse> GetAsync(
            Guid userId,
            int permissionsVersion,
            CancellationToken ct);
    }
}
