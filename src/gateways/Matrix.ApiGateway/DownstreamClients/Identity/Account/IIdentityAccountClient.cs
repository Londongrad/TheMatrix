using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public interface IIdentityAccountClient
    {
        Task<HttpResponseMessage> ChangeAvatarAsync(
            Guid userId,
            IFormFile avatar,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    }
}
