using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Weather.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityProvisioningView> CreateProvisionedCityAsync(
            CreateCityRequest request,
            CancellationToken cancellationToken = default)
        {
            const string url = $"{CitiesEndpoint}/provisioning";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityProvisioningView>(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<SimulationKindCatalogItemView>> GetSimulationKindsAsync(
            CancellationToken cancellationToken = default)
        {
            const string url = $"{CitiesEndpoint}/simulation-kinds";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<SimulationKindCatalogItemView>>(
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<CityListItemView>> ListProvisioningCitiesAsync(
            CancellationToken cancellationToken = default)
        {
            const string url = $"{CitiesEndpoint}/provisioning";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<CityListItemView>>(
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityProvisioningStatusView> GetProvisioningStatusAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/provisioning";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityProvisioningStatusView>(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityWeatherView> GetWeatherAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/weather";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityWeatherView>(
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityPopulationBootstrapRestartedView> RestartPopulationBootstrapAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/population-bootstrap/retry";

            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: url,
                content: null,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityPopulationBootstrapRestartedView>(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityProvisioningView> RetryPopulationBootstrapProvisioningAsync(
            Guid cityId,
            RetryCityPopulationBootstrapProvisioningRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/population-bootstrap/retry-provisioning";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityProvisioningView>(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task CompletePopulationBootstrapAsync(
            Guid cityId,
            CompleteCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/population-bootstrap/complete";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken);
        }

        public async Task CompleteEconomyBootstrapAsync(
            Guid cityId,
            CompleteCityEconomyBootstrapRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/economy-bootstrap/complete";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken);
        }

        public async Task FailEconomyBootstrapAsync(
            Guid cityId,
            FailCityEconomyBootstrapRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/economy-bootstrap/fail";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken);
        }

        public async Task UpdateEnvironmentAsync(
            Guid cityId,
            UpdateCityEnvironmentRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{CitiesEndpoint}/{cityId}/environment";

            using HttpResponseMessage response = await _client.PutAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
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
                serviceName: DownstreamServiceNames.SimulationCore,
                cancellationToken: cancellationToken);
        }
    }
}
