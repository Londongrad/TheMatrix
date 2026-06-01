using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterTickEventTests
{
    [Fact]
    public async Task AddSimulationTickPhaseReachedAsync_WritesRuntimeScopedPhase()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddSimulationTickPhaseReachedAsync_WritesRuntimeScopedPhase));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(55);
        var writer = new SimulationCoreOutboxWriter(
            dbContext: dbContext,
            timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var simulationId = new SimulationId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var hostId = new SimulationHostId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var runtimeKey = new SimulationRuntimeKey(
            new SimulationScenarioKey("classic-city"),
            new SimulationHostTypeKey("city"));
        var host = new SimulationHost(
            simulationId,
            hostId,
            runtimeKey,
            SimulationHostState.Active,
            OutboxTestSupport.BaseUtc,
            null);
        var from = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(6));
        var to = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(7));
        TickId tickId = new(73);
        var phaseKey = new SimulationPhaseKey("resource-settlement");

        await writer.AddSimulationTickPhaseReachedAsync(
            host,
            from,
            to,
            tickId,
            SimSpeed.From(30m),
            phaseKey,
            CancellationToken.None);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
        SimulationTickPhaseReachedV1 payload =
            OutboxTestSupport.DeserializePayload<SimulationTickPhaseReachedV1>(message);

        Assert.Equal(SimulationCoreEventTypes.SimulationTickPhaseReachedV1, message.Type);
        Assert.Equal(simulationId.Value, payload.SimulationId);
        Assert.Equal(hostId.Value, payload.HostId);
        Assert.Equal(runtimeKey.ScenarioKey.Value, payload.ScenarioKey);
        Assert.Equal(runtimeKey.HostTypeKey.Value, payload.HostTypeKey);
        Assert.Equal(phaseKey.Value, payload.PhaseKey);
        Assert.Equal(from.ValueUtc, payload.FromSimTimeUtc);
        Assert.Equal(to.ValueUtc, payload.ToSimTimeUtc);
        Assert.Equal(tickId.Value, payload.TickId);
        Assert.Equal(30m, payload.SpeedMultiplier);
        Assert.Equal(
            $"simulation:{simulationId.Value:N}:host:{hostId.Value:N}:tick:{tickId.Value}",
            payload.CorrelationId);
        Assert.Equal($"{payload.CorrelationId}:phase:{phaseKey.Value}", payload.CausationId);
        Assert.Equal(occurredOnUtc.UtcDateTime, payload.OccurredOnUtc);
    }
}
