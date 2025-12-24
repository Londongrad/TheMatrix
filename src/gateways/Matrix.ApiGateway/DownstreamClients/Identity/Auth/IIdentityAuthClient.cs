using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;

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
