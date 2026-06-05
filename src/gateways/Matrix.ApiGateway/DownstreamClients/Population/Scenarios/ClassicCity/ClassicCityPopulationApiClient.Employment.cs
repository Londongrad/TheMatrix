using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient
    {
        public async Task<CityEmploymentCatalogDto> GetCityEmploymentCatalogAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/employment/catalog";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEmploymentCatalogDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEmploymentOperationResultDto> HireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/employment/hire";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEmploymentOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEmploymentOperationResultDto> FireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/employment/fire";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEmploymentOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEmploymentOperationResultDto> RetireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/employment/retire";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEmploymentOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
