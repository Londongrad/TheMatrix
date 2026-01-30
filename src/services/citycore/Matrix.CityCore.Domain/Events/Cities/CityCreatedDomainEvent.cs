using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityCreatedDomainEvent(
        CityId CityId,
        CityName Name,
        CityEnvironment Environment,
        CityGenerationSeed GenerationSeed,
        CityGenerationProfile GenerationProfile,
        DateTimeOffset CreatedAtUtc)
        : DomainEventBase;
}
