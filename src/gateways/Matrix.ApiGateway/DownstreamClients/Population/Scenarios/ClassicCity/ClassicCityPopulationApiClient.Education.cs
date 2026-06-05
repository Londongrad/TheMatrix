using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient
    {
        public async Task<CityEducationCatalogDto> GetCityEducationCatalogAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/education/catalog";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEducationCatalogDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEducationOperationResultDto> EnrollCityResidentAsync(
            Guid cityId,
            CityEducationOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/education/enroll";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEducationOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEducationOperationResultDto> GraduateCityResidentAsync(
            Guid cityId,
            CityEducationOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/education/graduate";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEducationOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEducationOperationResultDto> WithdrawCityResidentFromStudyAsync(
            Guid cityId,
            CityEducationOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/education/withdraw";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEducationOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
