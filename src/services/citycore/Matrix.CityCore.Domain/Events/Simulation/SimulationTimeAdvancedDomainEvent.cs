using Matrix.CityCore.Domain.Cities;
using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationTimeAdvancedDomainEvent(
        CityId CityId,
        SimTime From,
        SimTime To,
        TickId TickId,
        SimSpeed Speed) : DomainEventBase;
}
