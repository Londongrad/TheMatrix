using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationSpeedChangedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimSpeed From,
        SimSpeed To,
        SimTime AtSimTime) : DomainEventBase;
}
