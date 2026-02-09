using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityPopulationBootstrapFailedDomainEvent(
        CityId CityId,
        Guid OperationId,
        string FailureCode,
        DateTimeOffset FailedAtUtc)
        : DomainEventBase;
}
