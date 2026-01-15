using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationTimeJumpedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimTime From,
        SimTime To) : DomainEventBase;
}
