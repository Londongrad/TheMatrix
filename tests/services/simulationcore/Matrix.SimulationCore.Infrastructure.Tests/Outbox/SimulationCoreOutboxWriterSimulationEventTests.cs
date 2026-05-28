using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterSimulationEventTests
{
    private static readonly SimulationId SimulationId = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly SimulationHostId HostId = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly SimulationRuntimeKey RuntimeKey = new(
        new SimulationScenarioKey("classic-city"),
        new SimulationHostTypeKey("city"));

    [Fact]
    public async Task AddSimulationEventsAsync_WritesScenarioNeutralLifecycleMessages()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddSimulationEventsAsync_WritesScenarioNeutralLifecycleMessages));
        var writer = new SimulationCoreOutboxWriter(
            dbContext: dbContext,
            timeProvider: OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));
        DateTimeOffset archivedAtUtc = OutboxTestSupport.BaseUtc.AddHours(1);
        DateTimeOffset deletedAtUtc = OutboxTestSupport.BaseUtc.AddHours(2);

        await writer.AddSimulationEventsAsync(
            domainEvents:
            [
                new SimulationCreatedDomainEvent(
                    SimulationId,
                    HostId,
                    RuntimeKey,
                    new SimulationSeed("seed-42"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    new SimulationModelVersion("classic-city-v1"),
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    SimulationHostState.Provisioning,
                    OutboxTestSupport.BaseUtc),
                new SimulationArchivedDomainEvent(SimulationId, HostId, RuntimeKey, archivedAtUtc),
                new SimulationDeletedDomainEvent(SimulationId, HostId, RuntimeKey, deletedAtUtc)
            ],
            cancellationToken: CancellationToken.None);
        await dbContext.SaveChangesAsync();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
           .AsNoTracking()
           .ToListAsync();

        Assert.Equal(3, messages.Count);
        SimulationCreatedV1 created = Deserialize<SimulationCreatedV1>(
            messages,
            IntegrationEventTypes.SimulationCreatedV1);
        Assert.Equal(SimulationId.Value, created.SimulationId);
        Assert.Equal(HostId.Value, created.HostId);
        Assert.Equal(RuntimeKey.ScenarioKey.Value, created.ScenarioKey);
        Assert.Equal(RuntimeKey.HostTypeKey.Value, created.HostTypeKey);
        Assert.Equal("seed-42", created.Seed);
        Assert.Equal("Provisioning", created.State);

        SimulationArchivedV1 archived = Deserialize<SimulationArchivedV1>(
            messages,
            IntegrationEventTypes.SimulationArchivedV1);
        Assert.Equal(archivedAtUtc, archived.ArchivedAtUtc);

        SimulationDeletedV1 deleted = Deserialize<SimulationDeletedV1>(
            messages,
            IntegrationEventTypes.SimulationDeletedV1);
        Assert.Equal(deletedAtUtc, deleted.DeletedAtUtc);
    }

    private static T Deserialize<T>(IEnumerable<OutboxMessage> messages, string type)
        where T : notnull
    {
        return OutboxTestSupport.DeserializePayload<T>(
            Assert.Single(messages, message => message.Type == type));
    }
}
