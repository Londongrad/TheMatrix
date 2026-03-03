using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Account
{
    public interface IIdentityAccountClient
    {
        Task<ChangeAvatarResponse> ChangeAvatarAsync(
            IFormFile avatar,
            CancellationToken cancellationToken = default);

        Task<ChangeDisplayNameResponse> ChangeDisplayNameAsync(
            ChangeDisplayNameRequest request,
            CancellationToken cancellationToken = default);

        Task<ChangeUsernameResponse> ChangeUsernameAsync(
            ChangeUsernameRequest request,
            CancellationToken cancellationToken = default);

        Task<ChangeEmailResponse> ChangeEmailAsync(
            ChangeEmailRequest request,
            CancellationToken cancellationToken = default);

        Task<ChangeAvatarResponse> ClearAvatarAsync(
            CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default);

        Task ResendPendingEmailChangeAsync(
            CancellationToken cancellationToken = default);

        Task CancelPendingEmailChangeAsync(
            CancellationToken cancellationToken = default);

        Task DeleteAccountAsync(
            DeleteAccountRequest request,
            CancellationToken cancellationToken = default);

        Task<UserProfileResponse> GetProfileAsync(CancellationToken cancellationToken);

        Task<PagedResult<SecurityActivityItemResponse>> GetSecurityActivityPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);
    }
}
