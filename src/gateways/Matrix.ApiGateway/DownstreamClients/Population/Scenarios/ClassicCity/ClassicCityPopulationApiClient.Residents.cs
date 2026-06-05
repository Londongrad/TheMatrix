using System.Globalization;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient
    {
        public async Task<PagedResult<PersonDto>> GetCityResidentsPageAsync(
            Guid cityId,
            DateOnly currentDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string currentDateValue = Uri.EscapeDataString(
                stringToEscape: currentDate.ToString(
                    format: "yyyy-MM-dd",
                    provider: CultureInfo.InvariantCulture));
            string url =
                $"{PopulationBaseEndpoint}/cities/{cityId}/residents?currentDate={currentDateValue}&pageNumber={pageNumber}&pageSize={pageSize}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);

            PagedResult<PersonDto>? result = await response.Content
               .ReadFromJsonAsync<PagedResult<PersonDto>>(cancellationToken: cancellationToken);

            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        public async Task<CityResidentDetailsDto> GetCityResidentDetailsAsync(
            Guid cityId,
            Guid personId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            string currentDateValue = Uri.EscapeDataString(
                stringToEscape: currentDate.ToString(
                    format: "yyyy-MM-dd",
                    provider: CultureInfo.InvariantCulture));
            string url =
                $"{PopulationBaseEndpoint}/cities/{cityId}/residents/{personId}?currentDate={currentDateValue}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityResidentDetailsDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
