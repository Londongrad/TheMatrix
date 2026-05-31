using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed record SeedCityStockpilesCommand(
        Guid CityId,
        DateTimeOffset CreatedAtUtc,
        string DevelopmentLevel) : IRequest<SeedCityStockpilesResult>;
}
