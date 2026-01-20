using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Common;
using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Domain.Events.Simulation
{
    public sealed record SimulationPausedDomainEvent(
        CityId CityId,
        TickId TickId,
        SimTime AtSimTime) : DomainEventBase;
}
