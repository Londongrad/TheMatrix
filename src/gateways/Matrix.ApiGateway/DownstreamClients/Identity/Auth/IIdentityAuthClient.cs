using Matrix.Identity.Contracts.Auth.Requests;
using Matrix.Identity.Contracts.Auth.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public interface IIdentityAuthClient
    {
        Task<RegisterResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken ct = default);

        Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken ct = default);

        Task<LoginResponse> RefreshAsync(
            RefreshRequest request,
            CancellationToken ct = default);

        Task LogoutAsync(
            LogoutRequest request,
            CancellationToken ct = default);
    }
}
