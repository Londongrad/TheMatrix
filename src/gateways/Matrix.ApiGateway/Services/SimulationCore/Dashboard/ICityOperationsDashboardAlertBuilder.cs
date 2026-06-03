using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal interface ICityOperationsDashboardAlertBuilder
    {
        CityOperationsDashboardAlerts Build(IReadOnlyList<CityOperationalSnapshot> snapshots);
    }
}
