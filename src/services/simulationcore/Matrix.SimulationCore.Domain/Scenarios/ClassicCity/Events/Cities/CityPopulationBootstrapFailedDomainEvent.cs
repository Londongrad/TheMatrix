using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityPopulationBootstrapFailedDomainEvent(
        CityId CityId,
        Guid OperationId,
        string FailureCode,
        DateTimeOffset FailedAtUtc)
        : DomainEventBase;
}
