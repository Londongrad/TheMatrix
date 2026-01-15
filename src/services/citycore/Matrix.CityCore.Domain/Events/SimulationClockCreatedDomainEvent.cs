using Matrix.CityCore.Domain.Enums;
using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events
{
    public sealed record SimulationClockCreatedDomainEvent(
        CityId CityId,
        SimTime StartTime,
        SimSpeed Speed,
        ClockState State,
        TickId TickId) : DomainEventBase;
}
