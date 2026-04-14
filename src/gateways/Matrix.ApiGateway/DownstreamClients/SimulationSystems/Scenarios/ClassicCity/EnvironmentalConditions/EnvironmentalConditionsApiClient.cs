using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions
{
    internal sealed class EnvironmentalConditionsApiClient(HttpClient client) : IEnvironmentalConditionsApiClient
    {
        private const string EnvironmentalConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/environmental-conditions";
        private const string DistrictHeatingConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/heating/districts";
        private const string DistrictWaterConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/water-distribution/districts";
        private const string DistrictPowerConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/power-distribution/districts";
        private const string DistrictSanitationConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/sanitation/districts";
        private const string DistrictUtilityIncidentConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/utility-incidents/districts";

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
