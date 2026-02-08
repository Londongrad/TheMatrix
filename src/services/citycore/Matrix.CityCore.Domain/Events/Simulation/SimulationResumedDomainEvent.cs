using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationResumedDomainEvent(
        SimulationId SimulationId,
        CityId CityId,
        TickId TickId,
        SimTime AtSimTime) : DomainEventBase;
}
