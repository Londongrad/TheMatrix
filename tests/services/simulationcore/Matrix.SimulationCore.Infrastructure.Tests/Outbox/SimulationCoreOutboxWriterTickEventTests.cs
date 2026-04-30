using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterTickEventTests
{
    [Fact]
    public async Task AddCityTimeAdvancedAsync_WritesMessageWithTickContext()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCityTimeAdvancedAsync_WritesMessageWithTickContext));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(35);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var cityId = new CityId(Guid.Parse("66666666-6666-6666-6666-666666666666"));
        var simulationId = new SimulationId(Guid.Parse("77777777-7777-7777-7777-777777777777"));
        SimTime from = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(2));
        SimTime to = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(3));
        TickId tickId = new(42);
        SimSpeed speed = SimSpeed.From(60m);

        await writer.AddCityTimeAdvancedAsync(
            cityId: cityId,
            simulationId: simulationId,
            simulationKind: SimulationKind.ClassicCity,
            from: from,
            to: to,
            tickId: tickId,
            speed: speed,
            phase: CityTickPhase.DispatchExecution,
            cancellationToken: CancellationToken.None);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
        CityTimeAdvancedV1 payload = OutboxTestSupport.DeserializePayload<CityTimeAdvancedV1>(message);

        Assert.Equal(IntegrationEventTypes.CityTimeAdvancedV1, message.Type);
        Assert.Equal(occurredOnUtc.UtcDateTime, message.OccurredOnUtc);
        Assert.Equal(cityId.Value, payload.CityId);
        Assert.Equal(from.ValueUtc, payload.FromSimTimeUtc);
        Assert.Equal(to.ValueUtc, payload.ToSimTimeUtc);
        Assert.Equal(tickId.Value, payload.TickId);
        Assert.Equal(speed.Multiplier, payload.SpeedMultiplier);
        Assert.Equal(occurredOnUtc.UtcDateTime, payload.OccurredOnUtc);
        Assert.Equal(simulationId.Value, payload.TickContext.SimulationId);
        Assert.Equal(cityId.Value, payload.TickContext.CityId);
        Assert.Equal(SimulationKind.ClassicCity.ToString(), payload.TickContext.SimulationKind);
        Assert.Equal(tickId.Value, payload.TickContext.TickId);
        Assert.Equal(to.ValueUtc, payload.TickContext.EffectiveSimTimeUtc);
        Assert.Equal(CityTickPhaseV1.DispatchExecution, payload.TickContext.Phase);
        Assert.Equal(1, payload.TickContext.ModelVersion);
        Assert.Equal(
            $"simulation:{simulationId.Value:N}:city:{cityId.Value:N}:tick:{tickId.Value}",
            payload.TickContext.CorrelationId);
        Assert.Equal(
            $"{payload.TickContext.CorrelationId}:phase:{CityTickPhase.DispatchExecution}",
            payload.TickContext.CausationId);
    }

    [Fact]
    public async Task AddCityTickPhaseReachedAsync_WritesMessageWithMappedPhase()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCityTickPhaseReachedAsync_WritesMessageWithMappedPhase));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(45);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var cityId = new CityId(Guid.Parse("88888888-8888-8888-8888-888888888888"));
        var simulationId = new SimulationId(Guid.Parse("99999999-9999-9999-9999-999999999999"));
        SimTime from = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(4));
        SimTime to = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(5));
        TickId tickId = new(64);
        SimSpeed speed = SimSpeed.RealTime();

        await writer.AddCityTickPhaseReachedAsync(
            cityId: cityId,
            simulationId: simulationId,
            simulationKind: SimulationKind.ClassicCity,
            from: from,
            to: to,
            tickId: tickId,
            speed: speed,
            phase: CityTickPhase.TickCompleted,
            cancellationToken: CancellationToken.None);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
        CityTickPhaseReachedV1 payload = OutboxTestSupport.DeserializePayload<CityTickPhaseReachedV1>(message);

        Assert.Equal(IntegrationEventTypes.CityTickPhaseReachedV1, message.Type);
        Assert.Equal(occurredOnUtc.UtcDateTime, message.OccurredOnUtc);
        Assert.Equal(cityId.Value, payload.CityId);
        Assert.Equal(from.ValueUtc, payload.FromSimTimeUtc);
        Assert.Equal(to.ValueUtc, payload.ToSimTimeUtc);
        Assert.Equal(tickId.Value, payload.TickId);
        Assert.Equal(speed.Multiplier, payload.SpeedMultiplier);
        Assert.Equal(occurredOnUtc.UtcDateTime, payload.OccurredOnUtc);
        Assert.Equal(CityTickPhaseV1.TickCompleted, payload.TickContext.Phase);
        Assert.Equal(to.ValueUtc, payload.TickContext.EffectiveSimTimeUtc);
        Assert.Equal(
            $"simulation:{simulationId.Value:N}:city:{cityId.Value:N}:tick:{tickId.Value}",
            payload.TickContext.CorrelationId);
        Assert.Equal(
            $"{payload.TickContext.CorrelationId}:phase:{CityTickPhase.TickCompleted}",
            payload.TickContext.CausationId);
    }
}
