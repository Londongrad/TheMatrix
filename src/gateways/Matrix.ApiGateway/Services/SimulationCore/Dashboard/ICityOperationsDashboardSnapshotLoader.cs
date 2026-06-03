using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal interface ICityOperationsDashboardSnapshotLoader
    {
        Task<IReadOnlyList<CityOperationalSnapshot>> LoadReadyClassicCitySnapshotsAsync(
            IReadOnlyList<CityListItemView> allCities,
            CancellationToken cancellationToken);
    }
}
