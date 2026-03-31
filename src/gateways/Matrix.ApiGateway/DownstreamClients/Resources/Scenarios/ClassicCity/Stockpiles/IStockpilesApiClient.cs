using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;

namespace Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles
{
    public interface IStockpilesApiClient
    {
        Task<CityStockpilesView?> GetCityStockpilesAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
