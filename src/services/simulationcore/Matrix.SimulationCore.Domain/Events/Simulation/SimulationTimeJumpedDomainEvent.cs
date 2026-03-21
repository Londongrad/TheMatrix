using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation
{
    public sealed record SimulationTimeJumpedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        TickId TickId,
        SimTime From,
        SimTime To) : DomainEventBase;
}
