using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
using Microsoft.AspNetCore.WebUtilities;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions
{
    public sealed class IdentitySessionsApiClient(HttpClient httpClient) : IIdentitySessionsClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Constants ]

        private const string ServiceName = DownstreamServiceNames.Identity;
        private const string Base = "/api/me/sessions";

        #endregion [ Constants ]

        #region [ Methods ]

        public async Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: Base,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<SessionResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: Base);
        }

        public async Task<PagedResult<SessionResponse>> GetSessionHistoryPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string url = QueryHelpers.AddQueryString(
                uri: $"{Base}/history",
                queryString: new Dictionary<string, string?>
                {
                    ["pageNumber"] = pageNumber.ToString(),
                    ["pageSize"] = pageSize.ToString()
                });

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<PagedResult<SessionResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
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

        public async Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.DeleteAsync(
                requestUri: $"{Base}/others",
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
