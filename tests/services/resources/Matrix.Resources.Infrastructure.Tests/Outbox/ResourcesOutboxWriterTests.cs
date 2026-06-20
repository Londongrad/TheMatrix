using System.Text.Json;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Resources.Infrastructure.Outbox;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Resources.Infrastructure.Persistence;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Outbox
{
    public sealed class ResourcesOutboxWriterTests
    {
        [Fact]
        public void ClassicCityOutboxContributor_ContainsKnownResourcesEvents()
        {
            var registry = new OutboxEventTypeRegistry([new ClassicCityOutboxEventTypeContributor()]);

            Assert.Equal(
                expected: typeof(ClassicCityStockpileSnapshotV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityStockpileSnapshotV1));
            Assert.Equal(
                expected: typeof(ClassicCityOperationalExpenseIncurredV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1));
        }

        [Fact]
        public async Task CityStockpileSnapshotOutboxWriter_AddsSerializedOutboxMessage()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var writer = new CityStockpileSnapshotOutboxWriter(dbContext);
            ClassicCityStockpileSnapshotV1 snapshot = CreateStockpileSnapshotEvent();

            await writer.AddClassicCityStockpileSnapshotAsync(
                snapshot: snapshot,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.ClassicCityStockpileSnapshotV1,
                actual: message.Type);
            Assert.Equal(
                expected: snapshot.OccurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);

            ClassicCityStockpileSnapshotV1? payload = JsonSerializer.Deserialize<ClassicCityStockpileSnapshotV1>(
                json: message.PayloadJson,
                options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(payload);
            Assert.Equal(
                expected: snapshot.CityId,
                actual: payload!.CityId);
            Assert.Equal(
                expected: snapshot.EffectiveTickId,
                actual: payload.EffectiveTickId);
        }

        [Fact]
        public async Task CityOperationalExpenseOutboxWriter_AddsSerializedOutboxMessage()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
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
    }
}
