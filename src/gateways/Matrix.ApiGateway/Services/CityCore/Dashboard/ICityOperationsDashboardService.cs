using Matrix.ApiGateway.Contracts.CityCore.Dashboard;

namespace Matrix.ApiGateway.Services.CityCore.Dashboard
{
    public interface ICityOperationsDashboardService
    {
        Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken);
    }
}
