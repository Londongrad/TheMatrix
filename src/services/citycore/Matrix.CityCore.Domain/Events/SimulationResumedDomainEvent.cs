using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationResumedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimTime AtSimTime) : DomainEventBase;
}
