using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Outbox;

public sealed class CityOperationalBudgetSignalOutboxWriterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishClassicCityOperationalBudgetPressureSnapshotAsync_AddsOutboxMessage()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        DateTimeOffset effectiveAtUtc = new(2048, 5, 6, 11, 0, 0, TimeSpan.Zero);
        DateTimeOffset occurredAtUtc = new(2048, 5, 6, 11, 5, 0, TimeSpan.Zero);
        var snapshot = new CityOperationalBudgetPressureDto(
            CityId: cityId,
            EffectiveTickId: 42,
            EffectiveAtUtc: effectiveAtUtc,
            UnitKind: "Currency",
            UnitCode: "MNY",
            UnitDisplayName: "Money",
            UnitSymbol: "$",
            Balance: 500m,
            TotalCityExpenses: 120m,
            MunicipalOperationsExpenses: 40m,
            InfrastructureOperationsExpenses: 30m,
            EmergencyOperationsExpenses: 10m,
            GeneralAvailableAmount: 300m,
            OperationsAvailableAmount: 200m,
            InfrastructureAvailableAmount: 150m,
            HealthcareAvailableAmount: 90m,
            GeneralAuthorizationLevel: "Open",
            OperationsAuthorizationLevel: "Watch",
            InfrastructureAuthorizationLevel: "Stable",
            HealthcareAuthorizationLevel: "Protected",
            LastMunicipalExpenseAtUtc: "2048-05-06T11:00:00.0000000+00:00",
            PressureIndex: 0.25m);

        await using var dbContext = CreateDbContext();
        var writer = new CityOperationalBudgetSignalOutboxWriter(dbContext);

        await writer.PublishClassicCityOperationalBudgetPressureSnapshotAsync(snapshot, effectiveAtUtc, occurredAtUtc);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        var payload = JsonSerializer.Deserialize<ClassicCityOperationalBudgetPressureSnapshotV1>(message.PayloadJson, JsonOptions);
        Assert.Equal(EconomyOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1, message.Type);
        Assert.NotNull(payload);
        Assert.Equal(cityId, payload.CityId);
        Assert.Equal(42, payload.EffectiveTickId);
        Assert.Equal(500m, payload.Balance);
        Assert.Equal(occurredAtUtc.UtcDateTime, message.OccurredOnUtc);
    }
}
