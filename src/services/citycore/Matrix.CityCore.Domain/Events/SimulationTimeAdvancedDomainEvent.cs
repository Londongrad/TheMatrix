using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationTimeAdvancedDomainEvent(
        CityId CityId,
        SimTime From,
        SimTime To,
        TickId TickId,
        SimSpeed Speed) : DomainEventBase;
}
