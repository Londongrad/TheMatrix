namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    internal interface ICityOperationsDashboardAlertBuilder
    {
        CityOperationsDashboardAlerts Build(IReadOnlyList<CityOperationalSnapshot> snapshots);
    }
}
