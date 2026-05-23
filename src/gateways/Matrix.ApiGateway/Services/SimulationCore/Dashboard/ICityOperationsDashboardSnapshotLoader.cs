using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal interface ICityOperationsDashboardSnapshotLoader
    {
        Task<IReadOnlyList<CityOperationalSnapshot>> LoadReadyClassicCitySnapshotsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken);
    }
}
