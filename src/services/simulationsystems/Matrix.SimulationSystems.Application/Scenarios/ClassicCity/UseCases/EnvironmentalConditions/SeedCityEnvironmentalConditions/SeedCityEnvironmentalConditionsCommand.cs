using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions
{
    public sealed record SeedCityEnvironmentalConditionsCommand(
        Guid CityId,
        DateTimeOffset CreatedAtUtc,
        string SimulationKind,
        string DevelopmentLevel) : IRequest<SeedCityEnvironmentalConditionsResult>;
}
