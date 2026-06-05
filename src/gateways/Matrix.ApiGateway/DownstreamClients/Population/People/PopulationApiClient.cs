using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.People
{
    public sealed class PopulationApiClient(HttpClient client)
        : IPopulationApiClient
    {
        #region [ Fields ]

        private readonly HttpClient _client = client;

        #endregion [ Fields ]

        #region [ Methods ]

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

        public async Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string query = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            string url = GetPagedEndpoint + query;

            using HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: url,
                    cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);

            PagedResult<PersonDto>? result = await response.Content
               .ReadFromJsonAsync<PagedResult<PersonDto>>(cancellationToken: cancellationToken);

            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = DownstreamServiceNames.Population;

        private const string PopulationBaseEndpoint = "/api/population";

        private const string GetPagedEndpoint = PopulationBaseEndpoint + "/citizens";

        #endregion [ Constants ]
    }
}
