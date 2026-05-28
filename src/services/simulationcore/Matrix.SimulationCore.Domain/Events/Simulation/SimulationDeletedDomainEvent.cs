using Matrix.BuildingBlocks.Domain.Events;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation;

public sealed record SimulationDeletedDomainEvent(
    SimulationId SimulationId,
    SimulationHostId HostId,
    SimulationRuntimeKey RuntimeKey,
    DateTimeOffset DeletedAtUtc) : DomainEventBase;
