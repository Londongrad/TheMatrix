using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public interface ICityOperationsDashboardService
    {
        Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken);
    }
}
