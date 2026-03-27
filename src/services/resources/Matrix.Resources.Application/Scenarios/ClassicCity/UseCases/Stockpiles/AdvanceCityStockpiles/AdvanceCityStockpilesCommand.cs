using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed record AdvanceCityStockpilesCommand(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc) : IRequest<AdvanceCityStockpilesResult>;
}
