using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth
{
    public interface IIdentityAuthClient
    {
        Task<RegisterResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        Task<LoginResponse> RefreshAsync(
            RefreshRequest request,
            CancellationToken cancellationToken = default);

        Task SendEmailConfirmationAsync(
            SendEmailConfirmationRequest request,
            CancellationToken cancellationToken = default);

        Task ConfirmEmailAsync(
            ConfirmEmailRequest request,
            CancellationToken cancellationToken = default);

        Task ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default);
    }
}
