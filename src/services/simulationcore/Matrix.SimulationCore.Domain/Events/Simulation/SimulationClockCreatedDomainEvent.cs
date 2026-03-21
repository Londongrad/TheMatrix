using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation
{
    public sealed record SimulationClockCreatedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        SimTime StartTime,
        SimSpeed Speed,
        ClockState State,
        TickId TickId) : DomainEventBase;
}
