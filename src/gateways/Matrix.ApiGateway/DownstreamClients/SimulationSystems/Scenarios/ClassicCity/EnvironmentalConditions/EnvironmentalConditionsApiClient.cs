using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions
{
    internal sealed class EnvironmentalConditionsApiClient(HttpClient client) : IEnvironmentalConditionsApiClient
    {
        private const string EnvironmentalConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.EnvironmentalConditionsSegment;

        private const string DistrictHeatingConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.HeatingSegment + "/districts";

        private const string DistrictWaterConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.WaterDistributionSegment + "/districts";

        private const string DistrictPowerConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.PowerDistributionSegment + "/districts";

        private const string DistrictSanitationConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.SanitationSegment + "/districts";

        private const string DistrictUtilityIncidentConditionsEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.UtilityIncidentsSegment + "/districts";

        private const string UtilityIncidentResponseDispatchEndpointTemplate =
            ClassicCitySimulationSystemsApiRoutes.CitiesPath + "/{0}/" +
            ClassicCitySimulationSystemsApiRoutes.UtilityIncidentsSegment + "/response-dispatch";

        private readonly HttpClient _client = client;

        public async Task<CityEnvironmentalConditionsView?> GetCityEnvironmentalConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: EnvironmentalConditionsEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            return await response.ReadJsonOrThrowDownstreamAsync<CityEnvironmentalConditionsView>(
                serviceName: DownstreamServiceNames.SimulationSystems,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public Task<CityDistrictHeatingConditionsView?> GetCityDistrictHeatingConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return GetOptionalAsync<CityDistrictHeatingConditionsView>(
                url: string.Format(
                    format: DistrictHeatingConditionsEndpointTemplate,
                    arg0: cityId),
                cancellationToken: cancellationToken);
        }

        public Task<CityDistrictWaterDistributionConditionsView?> GetCityDistrictWaterDistributionConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return GetOptionalAsync<CityDistrictWaterDistributionConditionsView>(
                url: string.Format(
                    format: DistrictWaterConditionsEndpointTemplate,
                    arg0: cityId),
                cancellationToken: cancellationToken);
        }

        public Task<CityDistrictPowerDistributionConditionsView?> GetCityDistrictPowerDistributionConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return GetOptionalAsync<CityDistrictPowerDistributionConditionsView>(
                url: string.Format(
                    format: DistrictPowerConditionsEndpointTemplate,
                    arg0: cityId),
                cancellationToken: cancellationToken);
        }

        public Task<CityDistrictSanitationConditionsView?> GetCityDistrictSanitationConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return GetOptionalAsync<CityDistrictSanitationConditionsView>(
                url: string.Format(
                    format: DistrictSanitationConditionsEndpointTemplate,
                    arg0: cityId),
                cancellationToken: cancellationToken);
        }

        public Task<CityDistrictUtilityIncidentConditionsView?> GetCityDistrictUtilityIncidentConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return GetOptionalAsync<CityDistrictUtilityIncidentConditionsView>(
                url: string.Format(
                    format: DistrictUtilityIncidentConditionsEndpointTemplate,
                    arg0: cityId),
                cancellationToken: cancellationToken);
        }

        public async Task<CityUtilityIncidentStatusView> DispatchCityUtilityIncidentResponseAsync(
            Guid cityId,
            DispatchCityUtilityIncidentResponseRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: UtilityIncidentResponseDispatchEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityUtilityIncidentStatusView>(
                serviceName: DownstreamServiceNames.SimulationSystems,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        private async Task<T?> GetOptionalAsync<T>(
            string url,
            CancellationToken cancellationToken)
            where T : class
        {
            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            return await response.ReadJsonOrThrowDownstreamAsync<T>(
                serviceName: DownstreamServiceNames.SimulationSystems,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
