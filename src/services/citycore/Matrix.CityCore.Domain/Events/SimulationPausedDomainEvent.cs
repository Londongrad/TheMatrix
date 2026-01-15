using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationPausedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimTime AtSimTime) : DomainEventBase;
}
