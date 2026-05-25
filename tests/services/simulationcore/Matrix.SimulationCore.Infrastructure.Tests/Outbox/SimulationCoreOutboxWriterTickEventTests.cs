using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class SimulationCoreOutboxWriterTickEventTests
    {
        [Fact]
        public async Task AddCityTimeAdvancedAsync_WritesMessageWithTickContext()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddCityTimeAdvancedAsync_WritesMessageWithTickContext));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(35);
            var writer = new SimulationCoreOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            var cityId = new CityId(Guid.Parse("66666666-6666-6666-6666-666666666666"));
            var simulationId = new SimulationId(Guid.Parse("77777777-7777-7777-7777-777777777777"));
            var from = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(2));
            var to = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(3));
            TickId tickId = new(42);
            var speed = SimSpeed.From(60m);

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

            OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking()
               .SingleAsync();
            CityTimeAdvancedV1 payload = OutboxTestSupport.DeserializePayload<CityTimeAdvancedV1>(message);

            Assert.Equal(
                expected: IntegrationEventTypes.CityTimeAdvancedV1,
                actual: message.Type);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: message.OccurredOnUtc);
            Assert.Equal(
                expected: cityId.Value,
                actual: payload.CityId);
            Assert.Equal(
                expected: from.ValueUtc,
                actual: payload.FromSimTimeUtc);
            Assert.Equal(
                expected: to.ValueUtc,
                actual: payload.ToSimTimeUtc);
            Assert.Equal(
                expected: tickId.Value,
                actual: payload.TickId);
            Assert.Equal(
                expected: speed.Multiplier,
                actual: payload.SpeedMultiplier);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: payload.OccurredOnUtc);
            Assert.Equal(
                expected: simulationId.Value,
                actual: payload.TickContext.SimulationId);
            Assert.Equal(
                expected: cityId.Value,
                actual: payload.TickContext.CityId);
            Assert.Equal(
                expected: SimulationKind.ClassicCity.ToString(),
                actual: payload.TickContext.SimulationKind);
            Assert.Equal(
                expected: tickId.Value,
                actual: payload.TickContext.TickId);
            Assert.Equal(
                expected: to.ValueUtc,
                actual: payload.TickContext.EffectiveSimTimeUtc);
            Assert.Equal(
                expected: CityTickPhaseV1.DispatchExecution,
                actual: payload.TickContext.Phase);
            Assert.Equal(
                expected: 1,
                actual: payload.TickContext.ModelVersion);
            Assert.Equal(
                expected: $"simulation:{simulationId.Value:N}:city:{cityId.Value:N}:tick:{tickId.Value}",
                actual: payload.TickContext.CorrelationId);
            Assert.Equal(
                expected: $"{payload.TickContext.CorrelationId}:phase:{CityTickPhase.DispatchExecution}",
                actual: payload.TickContext.CausationId);
        }

        [Fact]
        public async Task AddCityTickPhaseReachedAsync_WritesMessageWithMappedPhase()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddCityTickPhaseReachedAsync_WritesMessageWithMappedPhase));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(45);
            var writer = new SimulationCoreOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            var cityId = new CityId(Guid.Parse("88888888-8888-8888-8888-888888888888"));
            var simulationId = new SimulationId(Guid.Parse("99999999-9999-9999-9999-999999999999"));
            var from = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(4));
            var to = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(5));
            TickId tickId = new(64);
            var speed = SimSpeed.RealTime();

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

            OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking()
               .SingleAsync();
            CityTickPhaseReachedV1 payload = OutboxTestSupport.DeserializePayload<CityTickPhaseReachedV1>(message);

            Assert.Equal(
                expected: IntegrationEventTypes.CityTickPhaseReachedV1,
                actual: message.Type);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: message.OccurredOnUtc);
            Assert.Equal(
                expected: cityId.Value,
                actual: payload.CityId);
            Assert.Equal(
                expected: from.ValueUtc,
                actual: payload.FromSimTimeUtc);
            Assert.Equal(
                expected: to.ValueUtc,
                actual: payload.ToSimTimeUtc);
            Assert.Equal(
                expected: tickId.Value,
                actual: payload.TickId);
            Assert.Equal(
                expected: speed.Multiplier,
                actual: payload.SpeedMultiplier);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: payload.OccurredOnUtc);
            Assert.Equal(
                expected: CityTickPhaseV1.TickCompleted,
                actual: payload.TickContext.Phase);
            Assert.Equal(
                expected: to.ValueUtc,
                actual: payload.TickContext.EffectiveSimTimeUtc);
            Assert.Equal(
                expected: $"simulation:{simulationId.Value:N}:city:{cityId.Value:N}:tick:{tickId.Value}",
                actual: payload.TickContext.CorrelationId);
            Assert.Equal(
                expected: $"{payload.TickContext.CorrelationId}:phase:{CityTickPhase.TickCompleted}",
                actual: payload.TickContext.CausationId);
        }
    }
}
