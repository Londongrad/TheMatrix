using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;

namespace Matrix.ApiGateway.Services.SimulationCore.Dashboard
{
    public interface ICityOperationsDashboardService
    {
        Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken);
    }
}
