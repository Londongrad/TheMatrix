using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities
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
