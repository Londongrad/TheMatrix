using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxWriterTests
    {
        [Fact]
        public async Task CityOperationalExpenseOutboxWriter_AddsSerializedOutboxMessage()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            var writer = new CityOperationalExpenseOutboxWriter(dbContext);
            ClassicCityOperationalExpenseIncurredV1 expense = CreateOperationalExpenseEvent();

            await writer.AddClassicCityOperationalExpenseAsync(
                expense: expense,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1,
                actual: message.Type);
            Assert.Equal(
                expected: expense.OccurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);

            ClassicCityOperationalExpenseIncurredV1? payload =
                JsonSerializer.Deserialize<ClassicCityOperationalExpenseIncurredV1>(
                    json: message.PayloadJson,
                    options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(payload);
            Assert.Equal(
                expected: expense.CityId,
                actual: payload!.CityId);
            Assert.Equal(
                expected: expense.OperationKind,
                actual: payload.OperationKind);
        }

        [Fact]
        public async Task CityPopulationLivingConditionsOutboxWriter_AddsSerializedOutboxMessage()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            var writer = new CityPopulationLivingConditionsOutboxWriter(dbContext);
            ClassicCityLivingConditionsSnapshotV1 snapshot = CreateLivingConditionsSnapshotEvent();

            await writer.AddClassicCityLivingConditionsSnapshotAsync(
                snapshot: snapshot,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1,
                actual: message.Type);
            Assert.Equal(
                expected: snapshot.OccurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);

            ClassicCityLivingConditionsSnapshotV1? payload =
                JsonSerializer.Deserialize<ClassicCityLivingConditionsSnapshotV1>(
                    json: message.PayloadJson,
                    options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(payload);
            Assert.Equal(
                expected: snapshot.CityId,
                actual: payload!.CityId);
            Assert.Equal(
                expected: snapshot.UtilityContinuityIndex,
                actual: payload.UtilityContinuityIndex);
        }

        [Fact]
        public async Task CitySystemsResourceDemandOutboxWriter_AddsSerializedOutboxMessage()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            var writer = new CitySystemsResourceDemandOutboxWriter(dbContext);
            ClassicCitySystemsResourceDemandSnapshotV1 snapshot = CreateSystemsResourceDemandSnapshotEvent();

            await writer.AddClassicCitySystemsResourceDemandAsync(
                snapshot: snapshot,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1,
                actual: message.Type);
            Assert.Equal(
                expected: snapshot.OccurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);

            ClassicCitySystemsResourceDemandSnapshotV1? payload =
                JsonSerializer.Deserialize<ClassicCitySystemsResourceDemandSnapshotV1>(
                    json: message.PayloadJson,
                    options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(payload);
            Assert.Equal(
                expected: snapshot.CityId,
                actual: payload!.CityId);
            Assert.Equal(
                expected: snapshot.OverallDemandPressureIndex,
                actual: payload.OverallDemandPressureIndex);
        }
    }
}
