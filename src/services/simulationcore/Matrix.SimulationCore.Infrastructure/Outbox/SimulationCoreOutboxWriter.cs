using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public sealed class SimulationCoreOutboxWriter(
        SimulationCoreDbContext dbContext,
        TimeProvider timeProvider) : ISimulationCoreOutboxWriter
    {
        public Task AddSimulationEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                OutboxMessage? message = domainEvent switch
                {
                    SimulationCreatedDomainEvent created => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.SimulationCreatedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationCreatedV1(
                            SimulationId: created.SimulationId.Value,
                            HostId: created.HostId.Value,
                            ScenarioKey: created.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: created.RuntimeKey.HostTypeKey.Value,
                            Seed: created.Seed.Value,
                            RunId: created.RunId,
                            ModelVersion: created.ModelVersion.Value,
                            ProvisioningCorrelationId: created.ProvisioningCorrelationId,
                            State: created.State.ToString(),
                            CreatedAtUtc: created.CreatedAtUtc)),
                    SimulationArchivedDomainEvent archived => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.SimulationArchivedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationArchivedV1(
                            SimulationId: archived.SimulationId.Value,
                            HostId: archived.HostId.Value,
                            ScenarioKey: archived.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: archived.RuntimeKey.HostTypeKey.Value,
                            ArchivedAtUtc: archived.ArchivedAtUtc)),
                    SimulationDeletedDomainEvent deleted => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.SimulationDeletedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationDeletedV1(
                            SimulationId: deleted.SimulationId.Value,
                            HostId: deleted.HostId.Value,
                            ScenarioKey: deleted.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: deleted.RuntimeKey.HostTypeKey.Value,
                            DeletedAtUtc: deleted.DeletedAtUtc)),
                    _ => null
                };

                if (message is not null)
                    dbContext.OutboxMessages.Add(message);
            }

            return Task.CompletedTask;
        }

        public Task AddSimulationTickPhaseReachedAsync(
            SimulationHost host,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            SimulationPhaseKey phaseKey,
            CancellationToken cancellationToken)
        {
            DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
            string correlationId =
                $"simulation:{host.SimulationId.Value:N}:host:{host.HostId.Value:N}:tick:{tickId.Value}";
            string causationId = $"{correlationId}:phase:{phaseKey.Value}";

            var integrationEvent = new SimulationTickPhaseReachedV1(
                SimulationId: host.SimulationId.Value,
                HostId: host.HostId.Value,
                ScenarioKey: host.RuntimeKey.ScenarioKey.Value,
                HostTypeKey: host.RuntimeKey.HostTypeKey.Value,
                PhaseKey: phaseKey.Value,
                FromSimTimeUtc: from.ValueUtc,
                ToSimTimeUtc: to.ValueUtc,
                TickId: tickId.Value,
                SpeedMultiplier: speed.Multiplier,
                ModelVersion: 1,
                CausationId: causationId,
                CorrelationId: correlationId,
                OccurredOnUtc: occurredOnUtc);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: SimulationCoreEventTypes.SimulationTickPhaseReachedV1,
                    occurredOnUtc: occurredOnUtc,
                    payload: integrationEvent));

            return Task.CompletedTask;
        }

        public Task AddCareFacilityProvisioningAsync(
            CareFacilityProvisioningBatch batch,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(batch);

            if (batch.Facilities.Count == 0)
                return Task.CompletedTask;

            var integrationEvent = new SimulationCareFacilityProvisioningBatchV1(
                SimulationHostId: batch.SimulationHostId,
                SourceRevision: batch.SourceRevision,
                SynchronizedAtUtc: batch.SynchronizedAtUtc,
                CorrelationId: batch.CorrelationId,
                BatchNumber: 1,
                TotalBatches: 1,
                Facilities: batch.Facilities
                   .Select(facility => new SimulationCareFacilityProvisioningV1(
                        FacilityId: facility.FacilityId,
                        Name: facility.Name,
                        Kind: facility.Kind,
                        LocationAnchorId: facility.LocationAnchorId,
                        DailyPatientCapacity: facility.DailyPatientCapacity,
                        IsActive: facility.IsActive))
                   .ToArray());

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: SimulationCoreEventTypes.SimulationCareFacilityProvisioningBatchV1,
                    occurredOnUtc: timeProvider.GetUtcNow().UtcDateTime,
                    payload: integrationEvent));

            return Task.CompletedTask;
        }
    }
}
