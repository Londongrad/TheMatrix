using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    public interface IClassicCityPopulationApiClient
    {
        Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
            InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityPopulationDashboardDto> GetCityPopulationDashboardAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityPopulationDistrictPressureDto> GetCityDistrictPressureAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<CityResidentSummaryDto>> GetCityResidentsPageAsync(
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

        Task<CityEmploymentCatalogDto> GetCityEmploymentCatalogAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityEmploymentOperationResultDto> HireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityEmploymentOperationResultDto> FireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityEmploymentOperationResultDto> RetireCityResidentAsync(
            Guid cityId,
            CityEmploymentOperationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityCivilRegistryOperationResultDto> RegisterCityMarriageAsync(
            Guid cityId,
            CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default);

        Task<CityCivilRegistryOperationResultDto> RegisterCityDivorceAsync(
            Guid cityId,
            CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default);
    }
}
