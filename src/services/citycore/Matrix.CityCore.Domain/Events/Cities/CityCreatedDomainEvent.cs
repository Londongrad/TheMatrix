using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;

namespace Matrix.CityCore.Domain.Events.Cities
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
