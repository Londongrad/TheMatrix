namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal interface ICityOperationsDashboardAlertBuilder
    {
        CityOperationsDashboardAlerts Build(IReadOnlyList<CityOperationalSnapshot> snapshots);
    }
}
