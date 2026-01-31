using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityPopulationBootstrapRestartedDomainEvent(
        CityId CityId,
        Guid PreviousOperationId,
        Guid OperationId,
        DateTimeOffset RestartedAtUtc)
        : DomainEventBase;
}
