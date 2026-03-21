using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation
{
    public sealed record SimulationTimeAdvancedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        SimTime From,
        SimTime To,
        TickId TickId,
        SimSpeed Speed) : DomainEventBase;
}
