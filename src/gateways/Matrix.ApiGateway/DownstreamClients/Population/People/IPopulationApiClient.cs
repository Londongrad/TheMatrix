using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.People
{
    public interface IPopulationApiClient
    {
        Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
            InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
            Guid cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);

        Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
