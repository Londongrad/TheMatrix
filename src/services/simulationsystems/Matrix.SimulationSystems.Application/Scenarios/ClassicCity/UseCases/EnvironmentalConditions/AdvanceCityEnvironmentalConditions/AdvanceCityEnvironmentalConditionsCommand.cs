using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions
{
    public sealed record AdvanceCityEnvironmentalConditionsCommand(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId) : IRequest<AdvanceCityEnvironmentalConditionsResult>;
}
