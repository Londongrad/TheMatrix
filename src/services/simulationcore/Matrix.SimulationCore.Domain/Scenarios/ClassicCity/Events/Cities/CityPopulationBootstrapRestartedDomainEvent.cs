using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityPopulationBootstrapRestartedDomainEvent(
        CityId CityId,
        Guid PreviousOperationId,
        Guid OperationId,
        DateTimeOffset RestartedAtUtc)
        : DomainEventBase;
}
