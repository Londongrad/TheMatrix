using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions
{
    public sealed record SeedCityEnvironmentalConditionsCommand(
        Guid CityId,
        DateTimeOffset CreatedAtUtc) : IRequest<SeedCityEnvironmentalConditionsResult>;
}
