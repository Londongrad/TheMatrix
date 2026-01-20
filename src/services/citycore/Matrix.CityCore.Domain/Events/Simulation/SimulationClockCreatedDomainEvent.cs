using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Common;
using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationClockCreatedDomainEvent(
        CityId CityId,
        SimTime StartTime,
        SimSpeed Speed,
        ClockState State,
        TickId TickId) : DomainEventBase;
}
