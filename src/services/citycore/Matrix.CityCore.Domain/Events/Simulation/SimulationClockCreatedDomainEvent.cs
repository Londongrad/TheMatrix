using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationClockCreatedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        SimTime StartTime,
        SimSpeed Speed,
        ClockState State,
        TickId TickId) : DomainEventBase;
}
