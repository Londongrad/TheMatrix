using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Common;
using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationSpeedChangedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimSpeed From,
        SimSpeed To,
        SimTime AtSimTime) : DomainEventBase;
}
