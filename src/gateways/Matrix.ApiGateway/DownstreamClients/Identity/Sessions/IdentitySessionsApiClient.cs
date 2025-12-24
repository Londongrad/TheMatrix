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

        public async Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: Base,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<SessionResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: Base);
        }

        public async Task RevokeSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.DeleteAsync(
                requestUri: $"{Base}/{sessionId}",
                cancellationToken: cancellationToken);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        public async Task RevokeAllSessionsAsync(CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.DeleteAsync(
                requestUri: Base,
                cancellationToken: cancellationToken);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        #endregion [ Methods ]
    }
}
