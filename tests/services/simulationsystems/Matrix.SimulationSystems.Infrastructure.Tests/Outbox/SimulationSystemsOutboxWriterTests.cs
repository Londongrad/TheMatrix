using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Infrastructure.Outbox;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Outbox;

public sealed class SimulationSystemsOutboxWriterTests
{
    [Fact]
    public void OutboxEventTypeMap_ContainsKnownSimulationSystemsEvents()
    {
        Assert.Equal(
            typeof(ClassicCityOperationalExpenseIncurredV1),
            OutboxEventTypeMap.Map[SimulationSystemsOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1]);
        Assert.Equal(
            typeof(ClassicCityLivingConditionsSnapshotV1),
            OutboxEventTypeMap.Map[SimulationSystemsOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1]);
        Assert.Equal(
            typeof(ClassicCitySystemsResourceDemandSnapshotV1),
            OutboxEventTypeMap.Map[SimulationSystemsOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1]);
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
        Assert.Equal(SimulationSystemsOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1, message.Type);
        Assert.Equal(expense.OccurredAtUtc.UtcDateTime, message.OccurredOnUtc);

        ClassicCityOperationalExpenseIncurredV1? payload = JsonSerializer.Deserialize<ClassicCityOperationalExpenseIncurredV1>(
            message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal(expense.CityId, payload!.CityId);
        Assert.Equal(expense.OperationKind, payload.OperationKind);
    }

    [Fact]
    public async Task CityPopulationLivingConditionsOutboxWriter_AddsSerializedOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var writer = new CityPopulationLivingConditionsOutboxWriter(dbContext);
        ClassicCityLivingConditionsSnapshotV1 snapshot = CreateLivingConditionsSnapshotEvent();

        await writer.AddClassicCityLivingConditionsSnapshotAsync(snapshot, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(SimulationSystemsOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1, message.Type);
        Assert.Equal(snapshot.OccurredAtUtc.UtcDateTime, message.OccurredOnUtc);

        ClassicCityLivingConditionsSnapshotV1? payload = JsonSerializer.Deserialize<ClassicCityLivingConditionsSnapshotV1>(
            message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal(snapshot.CityId, payload!.CityId);
        Assert.Equal(snapshot.UtilityContinuityIndex, payload.UtilityContinuityIndex);
    }

    [Fact]
    public async Task CitySystemsResourceDemandOutboxWriter_AddsSerializedOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var writer = new CitySystemsResourceDemandOutboxWriter(dbContext);
        ClassicCitySystemsResourceDemandSnapshotV1 snapshot = CreateSystemsResourceDemandSnapshotEvent();

        await writer.AddClassicCitySystemsResourceDemandAsync(snapshot, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(SimulationSystemsOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1, message.Type);
        Assert.Equal(snapshot.OccurredAtUtc.UtcDateTime, message.OccurredOnUtc);

        ClassicCitySystemsResourceDemandSnapshotV1? payload = JsonSerializer.Deserialize<ClassicCitySystemsResourceDemandSnapshotV1>(
            message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal(snapshot.CityId, payload!.CityId);
        Assert.Equal(snapshot.OverallDemandPressureIndex, payload.OverallDemandPressureIndex);
    }
}
