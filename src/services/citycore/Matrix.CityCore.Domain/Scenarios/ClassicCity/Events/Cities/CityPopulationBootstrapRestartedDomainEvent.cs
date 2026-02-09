using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityPopulationBootstrapRestartedDomainEvent(
        CityId CityId,
        Guid PreviousOperationId,
        Guid OperationId,
        DateTimeOffset RestartedAtUtc)
        : DomainEventBase;
}
