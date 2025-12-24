using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
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

        Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default);
    }
}
