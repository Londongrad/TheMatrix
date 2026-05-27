using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation
{
    public sealed record SimulationSpeedChangedDomainEvent(
        SimulationId SimulationId,
        TickId TickId,
        SimSpeed From,
        SimSpeed To,
        SimTime AtSimTime) : DomainEventBase;
}
