using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Person
{
    public sealed class PersonApiClient(HttpClient client) : IPersonApiClient
    {
        #region [ Fields ]

        private readonly HttpClient _client = client;

        #endregion [ Fields ]

        #region [ Constants ]

        private const string ServiceName = "Population";

        private const string Base = "/api/person";

        private const string KillSegment = "/kill";
        private const string ResurrectSegment = "/resurrect";

        #endregion [ Constants ]

        #region [ Methods ]

        public async Task<PersonDto> KillAsync(
            Guid personId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{Base}/{personId}" + $"{KillSegment}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                ct: cancellationToken);

            PersonDto? dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for Kill.");
        }

        public async Task<PersonDto> ResurrectAsync(
            Guid personId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{Base}/{personId}" + $"{ResurrectSegment}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                ct: cancellationToken);

            PersonDto? dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for Resurrect.");
        }

        #endregion [ Methods ]
    }
}
