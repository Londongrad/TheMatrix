using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public interface ICityOperationsDashboardHealthProbe
    {
        Task<IReadOnlyList<DashboardServiceHealthView>> ProbeAsync(CancellationToken cancellationToken);
    }
}
