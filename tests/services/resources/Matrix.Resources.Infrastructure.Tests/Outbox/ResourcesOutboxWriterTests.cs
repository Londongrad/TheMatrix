using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Resources.Infrastructure.Outbox;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Outbox;

public sealed class ResourcesOutboxWriterTests
{
    [Fact]
    public void OutboxEventTypeMap_ContainsKnownResourcesEvents()
    {
        Assert.Equal(typeof(ClassicCityStockpileSnapshotV1), OutboxEventTypeMap.Map[ResourcesOutboxEventTypes.ClassicCityStockpileSnapshotV1]);
        Assert.Equal(typeof(ClassicCityOperationalExpenseIncurredV1), OutboxEventTypeMap.Map[ResourcesOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1]);
    }

    [Fact]
    public async Task CityStockpileSnapshotOutboxWriter_AddsSerializedOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var writer = new CityStockpileSnapshotOutboxWriter(dbContext);
        ClassicCityStockpileSnapshotV1 snapshot = CreateStockpileSnapshotEvent();

        await writer.AddClassicCityStockpileSnapshotAsync(snapshot, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(ResourcesOutboxEventTypes.ClassicCityStockpileSnapshotV1, message.Type);
        Assert.Equal(snapshot.OccurredAtUtc.UtcDateTime, message.OccurredOnUtc);

        ClassicCityStockpileSnapshotV1? payload = JsonSerializer.Deserialize<ClassicCityStockpileSnapshotV1>(
            message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal(snapshot.CityId, payload!.CityId);
        Assert.Equal(snapshot.EffectiveTickId, payload.EffectiveTickId);
    }

    [Fact]
    public async Task CityOperationalExpenseOutboxWriter_AddsSerializedOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var writer = new CityOperationalExpenseOutboxWriter(dbContext);
        ClassicCityOperationalExpenseIncurredV1 expense = CreateOperationalExpenseEvent();

        await writer.AddClassicCityOperationalExpenseAsync(expense, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(ResourcesOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1, message.Type);
        Assert.Equal(expense.OccurredAtUtc.UtcDateTime, message.OccurredOnUtc);

        ClassicCityOperationalExpenseIncurredV1? payload = JsonSerializer.Deserialize<ClassicCityOperationalExpenseIncurredV1>(
            message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal(expense.CityId, payload!.CityId);
        Assert.Equal(expense.OperationKind, payload.OperationKind);
    }
}
