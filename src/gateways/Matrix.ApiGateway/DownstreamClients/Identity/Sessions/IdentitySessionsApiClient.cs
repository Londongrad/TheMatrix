using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Self.Sessions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Sessions
{
    public sealed class IdentitySessionsApiClient(HttpClient httpClient) : IIdentitySessionsClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Constants ]

        private const string ServiceName = "Identity";

        private const string Base = "/api/me/sessions";

        #endregion [ Constants ]

        #region [ Methods ]

        public async Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken ct = default)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: Base,
                cancellationToken: ct);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<SessionResponse>>(
                serviceName: ServiceName,
                ct: ct,
                requestUrl: Base);
        }

        public async Task RevokeSessionAsync(
            Guid sessionId,
            CancellationToken ct = default)
        {
            using HttpResponseMessage resp = await _httpClient.DeleteAsync(
                requestUri: $"{Base}/{sessionId}",
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                ct: ct);
        }

        public async Task RevokeAllSessionsAsync(CancellationToken ct = default)
        {
            using HttpResponseMessage resp = await _httpClient.DeleteAsync(
                requestUri: Base,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                ct: ct);
        }

        #endregion [ Methods ]
    }
}
