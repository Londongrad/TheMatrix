using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public interface IIdentityAccountClient
    {
        Task<HttpResponseMessage> ChangeAvatarAsync(
            string authorizationHeader,
            ChangeAvatarRequest request,
            CancellationToken ct = default);

        Task<HttpResponseMessage> ChangePasswordAsync(
            string authorizationHeader,
            ChangePasswordRequest request,
            CancellationToken ct = default);
    }
}