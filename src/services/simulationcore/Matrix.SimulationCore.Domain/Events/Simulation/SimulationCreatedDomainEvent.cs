using Matrix.BuildingBlocks.Domain.Events;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Events.Simulation;

public sealed record SimulationCreatedDomainEvent(
    SimulationId SimulationId,
    SimulationHostId HostId,
    SimulationRuntimeKey RuntimeKey,
    SimulationSeed Seed,
    Guid RunId,
    SimulationModelVersion ModelVersion,
    Guid? ProvisioningCorrelationId,
    SimulationHostState State,
    DateTimeOffset CreatedAtUtc) : DomainEventBase;
