using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    internal interface ICityOperationsDashboardSnapshotLoader
    {
        Task<IReadOnlyList<CityOperationalSnapshot>> LoadReadyClassicCitySnapshotsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken);
    }
}
