using System.Text.Json;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Economy.Infrastructure.Persistence;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class CityOperationalBudgetSignalOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task PublishClassicCityOperationalBudgetPressureSnapshotAsync_AddsOutboxMessage()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTimeOffset effectiveAtUtc = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 11,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            DateTimeOffset occurredAtUtc = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 11,
                minute: 5,
                second: 0,
                offset: TimeSpan.Zero);
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

            await using EconomyDbContext dbContext = CreateDbContext();
            var writer = new CityOperationalBudgetSignalOutboxWriter(dbContext);

            await writer.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                snapshot: snapshot,
                effectiveAtUtc: effectiveAtUtc,
                occurredAtUtc: occurredAtUtc);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            ClassicCityOperationalBudgetPressureSnapshotV1? payload =
                JsonSerializer.Deserialize<ClassicCityOperationalBudgetPressureSnapshotV1>(
                    json: message.PayloadJson,
                    options: JsonOptions);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1,
                actual: message.Type);
            Assert.NotNull(payload);
            Assert.Equal(
                expected: cityId,
                actual: payload.CityId);
            Assert.Equal(
                expected: 42,
                actual: payload.EffectiveTickId);
            Assert.Equal(
                expected: 500m,
                actual: payload.Balance);
            Assert.Equal(
                expected: occurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);
        }
    }
}
