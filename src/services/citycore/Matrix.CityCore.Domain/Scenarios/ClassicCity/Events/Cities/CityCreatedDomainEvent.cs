using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityCreatedDomainEvent(
        CityId CityId,
        CityName Name,
        SimulationKind SimulationKind,
        CityEnvironment Environment,
        CityGenerationSeed GenerationSeed,
        CityGenerationProfile GenerationProfile,
        Guid PopulationBootstrapOperationId,
        DateTimeOffset CreatedAtUtc)
        : DomainEventBase;
}
