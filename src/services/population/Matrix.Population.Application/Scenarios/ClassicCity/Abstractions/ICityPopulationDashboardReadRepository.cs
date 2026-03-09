using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationDashboardReadRepository
    {
        Task<CityPopulationDashboardSnapshotReadModel?> GetCurrentSnapshotAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<CityPopulationDashboardSnapshotReadModel?> GetSnapshotOnOrBeforeAsync(
            CityId cityId,
            DateOnly snapshotDate,
            CancellationToken cancellationToken = default);

        Task<CityPopulationDashboardEconomyReadModel> GetCurrentEconomySnapshotAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityPopulationActivityEventReadModel>> ListRecentActivityAsync(
            CityId cityId,
            int take,
            CancellationToken cancellationToken = default);
    }
}
