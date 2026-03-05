using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.People
{
    public interface IPopulationApiClient
    {
        Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
            InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<PersonDto>> GetCityResidentsPageAsync(
            Guid cityId,
            DateOnly currentDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<CityResidentDetailsDto> GetCityResidentDetailsAsync(
            Guid cityId,
            Guid personId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);

        Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
