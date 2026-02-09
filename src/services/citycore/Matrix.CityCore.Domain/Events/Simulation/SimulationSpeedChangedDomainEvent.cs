using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationSpeedChangedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        TickId TickId,
        SimSpeed From,
        SimSpeed To,
        SimTime AtSimTime) : DomainEventBase;
}
