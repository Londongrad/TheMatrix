using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public interface IIdentityApiClient
    {
        Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    }
}