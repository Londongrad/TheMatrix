using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient
    {
        public async Task<CityCivilRegistryOperationResultDto> RegisterCityMarriageAsync(
            Guid cityId,
            CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/civil-registry/marriages";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityCivilRegistryOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityCivilRegistryOperationResultDto> RegisterCityDivorceAsync(
            Guid cityId,
            CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/civil-registry/divorces";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityCivilRegistryOperationResultDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
