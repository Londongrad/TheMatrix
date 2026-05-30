using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityCreatedDomainEvent(
        CityId CityId,
        CityName Name,
        CityEnvironment Environment,
        CityGenerationSeed GenerationSeed,
        Guid RunId,
        ScenarioModelSetVersion ScenarioModelSetVersion,
        CityGenerationProfile GenerationProfile,
        Guid PopulationBootstrapOperationId,
        DateTimeOffset CreatedAtUtc)
        : DomainEventBase;
}
