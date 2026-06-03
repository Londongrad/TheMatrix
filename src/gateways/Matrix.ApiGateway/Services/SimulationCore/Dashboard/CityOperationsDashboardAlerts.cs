using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    internal sealed record CityOperationsDashboardAlerts(
        DashboardEnvironmentalAlertView[] EnvironmentalAlerts,
        DashboardPopulationDistrictPressureView[] PopulationDistrictAlerts,
        DashboardDistrictResponsePriorityView[] DistrictResponsePriorities,
        DashboardMobilityView[] MobilityAlerts,
        DashboardBudgetPressureView[] BudgetAlerts,
        DashboardTickFreshnessView[] TickFreshnessAlerts,
        DashboardPhaseProgressView[] PhaseProgressAlerts);
}
