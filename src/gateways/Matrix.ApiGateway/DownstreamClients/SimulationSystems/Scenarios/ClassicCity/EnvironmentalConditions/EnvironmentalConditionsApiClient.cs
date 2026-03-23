using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions
{
    internal sealed class EnvironmentalConditionsApiClient(HttpClient client) : IEnvironmentalConditionsApiClient
    {
        private const string EnvironmentalConditionsEndpointTemplate =
            "/api/classic-city/cities/{0}/environmental-conditions";

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
    }
}
