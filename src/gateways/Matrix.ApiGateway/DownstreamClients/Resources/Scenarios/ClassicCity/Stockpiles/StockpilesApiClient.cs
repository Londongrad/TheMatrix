using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;

namespace Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles
{
    internal sealed class StockpilesApiClient(HttpClient client) : IStockpilesApiClient
    {
        private const string StockpilesEndpointTemplate = "/api/classic-city/cities/{0}/stockpiles";

        private readonly HttpClient _client = client;

        public async Task<CityStockpilesView?> GetCityStockpilesAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: StockpilesEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            return await response.ReadJsonOrThrowDownstreamAsync<CityStockpilesView>(
                serviceName: DownstreamServiceNames.Resources,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
