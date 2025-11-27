using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public interface IIdentityApiClient
    {
        Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<HttpResponseMessage> LoginAsync(LoginRequest request, CancellationToken ct = default);

        Task<HttpResponseMessage> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
        Task LogoutAsync(RefreshRequest request, CancellationToken ct = default);
    }
}