using Matrix.Identity.Contracts.Account.Requests;
using Matrix.Identity.Contracts.Account.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public interface IIdentityAccountClient
    {
        Task<ChangeAvatarResponse> ChangeAvatarAsync(
            IFormFile avatar,
            CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<UserProfileResponse> GetProfileAsync(CancellationToken cancellationToken);
    }
}
