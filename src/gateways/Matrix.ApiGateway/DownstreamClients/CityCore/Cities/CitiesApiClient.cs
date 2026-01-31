using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using Matrix.CityCore.Contracts.Topology.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Cities
{
    internal sealed class CitiesApiClient(HttpClient client) : ICitiesApiClient
    {
        private const string CitiesEndpoint = "/api/cities";
        private readonly HttpClient _client = client;

        public async Task<CityCreatedView> CreateCityAsync(
            CreateCityRequest request,
            CancellationToken cancellationToken = default)
        {
            const string url = CitiesEndpoint;

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityCreatedView>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<CityListItemView>> ListCitiesAsync(
            bool includeArchived,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}?includeArchived={includeArchived.ToString().ToLowerInvariant()}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<CityListItemView>>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityView> GetCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityView>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<ResidentialBuildingView>> GetResidentialBuildingsAsync(
            Guid cityId,
            Guid? districtId = null,
            CancellationToken cancellationToken = default)
        {
            string url = districtId.HasValue
                ? $"{CitiesEndpoint}/{cityId}/residential-buildings?districtId={districtId.Value}"
                : $"{CitiesEndpoint}/{cityId}/residential-buildings";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<ResidentialBuildingView>>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task CompletePopulationBootstrapAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/population-bootstrap/complete";

            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: url,
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task FailPopulationBootstrapAsync(
            Guid cityId,
            FailCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/population-bootstrap/fail";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task RenameCityAsync(
            Guid cityId,
            RenameCityRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/name";

            using HttpResponseMessage response = await _client.PutAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task ArchiveCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/archive";

            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: url,
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}";

            using HttpResponseMessage response = await _client.DeleteAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }
    }
}
