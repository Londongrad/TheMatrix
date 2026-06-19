using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class CityEconomySettlementOutboxWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task AddCityDailySettlementAsync_AddsSettlementOutboxMessage()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new CityEconomySettlementOutboxWriter(dbContext);
            var settlement = new CityEconomyDailySettlementV1(
                CityId: Guid.Parse("7a0174f8-c6ad-4b14-9dc9-82e12b664149"),
                TickId: 15,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                SettledDays: 1,
                HouseholdCount: 11,
                ResidentCount: 33,
                GrossPayrollAmount: 120m,
                IncomeTaxAmount: 12m,
                NetPayrollAmount: 108m,
                RetailTurnoverAmount: 55m,
                RetailTaxAmount: 5.5m,
                HousingSpendAmount: 22m,
                CorrelationId: "corr:settlement",
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            await writer.AddCityDailySettlementAsync(settlement);
            await dbContext.SaveChangesAsync();

            OutboxMessage message = Assert.Single(dbContext.OutboxMessages);
            CityEconomyDailySettlementV1? payload =
                JsonSerializer.Deserialize<CityEconomyDailySettlementV1>(
                    json: message.PayloadJson,
                    options: JsonOptions);
            Assert.Equal(
                expected: ClassicCityOutboxEventTypes.CityEconomyDailySettlementV1,
                actual: message.Type);
            Assert.NotNull(payload);
            Assert.Equal(
                expected: settlement.CityId,
                actual: payload.CityId);
            Assert.Equal(
                expected: settlement.OccurredAtUtc.UtcDateTime,
                actual: message.OccurredOnUtc);
        }

        [Fact]
        public async Task AddClassicCitySyncBatchesAsync_AddsExpectedOutboxMessages()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            var writer = new CityEconomySettlementOutboxWriter(dbContext);
            DateTimeOffset occurredAtUtc = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 11,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var cityId = Guid.Parse("ef99102f-b180-493a-b56f-5f6e72c93f6a");

            await writer.AddClassicCityHouseholdAccountSyncBatchAsync(
                new ClassicCityHouseholdAccountSyncBatchV1(
                    CityId: cityId,
                    BatchNumber: 1,
                    TotalBatches: 2,
                    Households:
                    [
                        new ClassicCityHouseholdAccountSyncItemV1(
                            HouseholdId: Guid.Parse("3f11d500-f1f2-46a5-8fb0-e27938de0405"),
                            ExternalReferenceCode: "hh-1",
                            Name: "Household 1",
                            MemberCount: 3,
                            OpeningBalanceAmount: 50m,
                            IsHoused: true,
                            CreatedAtUtc: occurredAtUtc)
                    ],
                    CorrelationId: "corr:households",
                    OccurredAtUtc: occurredAtUtc));
            await writer.AddClassicCityWorkplaceBusinessSyncBatchAsync(
                new ClassicCityWorkplaceBusinessSyncBatchV1(
                    CityId: cityId,
                    BatchNumber: 1,
                    TotalBatches: 1,
                    Workplaces:
                    [
                        new ClassicCityWorkplaceBusinessSyncItemV1(
                            WorkplaceId: Guid.Parse("db871b9c-b7da-40ad-8a24-653c5c999ad3"),
                            ExternalReferenceCode: "wp-1",
                            Name: "Factory",
                            JobTitle: "Technician",
                            ActiveWorkerCount: 5)
                    ],
                    CorrelationId: "corr:workplaces",
                    OccurredAtUtc: occurredAtUtc));
            await writer.AddClassicCityWorkplacePayrollSettlementBatchAsync(
                new ClassicCityWorkplacePayrollSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 6),
                    SettledDays: 1,
                    BatchNumber: 1,
                    TotalBatches: 1,
                    Payrolls:
                    [
                        new ClassicCityWorkplacePayrollSettlementItemV1(
                            HouseholdId: Guid.Parse("3f11d500-f1f2-46a5-8fb0-e27938de0405"),
                            HouseholdExternalReferenceCode: "hh-1",
                            WorkplaceId: Guid.Parse("db871b9c-b7da-40ad-8a24-653c5c999ad3"),
                            WorkplaceExternalReferenceCode: "wp-1",
                            JobTitle: "Technician",
                            GrossPayrollAmount: 100m,
                            IncomeTaxAmount: 10m,
                            NetPayrollAmount: 90m)
                    ],
                    CorrelationId: "corr:payroll",
                    OccurredAtUtc: occurredAtUtc));
            await writer.AddClassicCityHouseholdCashflowSettlementBatchAsync(
                new ClassicCityHouseholdCashflowSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 6),
                    SettledDays: 1,
                    BatchNumber: 1,
                    TotalBatches: 1,
                    Households:
                    [
                        new ClassicCityHouseholdCashflowSettlementItemV1(
                            HouseholdId: Guid.Parse("3f11d500-f1f2-46a5-8fb0-e27938de0405"),
                            ExternalReferenceCode: "hh-1",
                            GrossPayrollAmount: 100m,
                            IncomeTaxAmount: 10m,
                            NetPayrollAmount: 90m,
                            RetailTurnoverAmount: 30m,
                            RetailTaxAmount: 3m,
                            RetailStoreSpendAmount: 20m,
                            ServiceSpendAmount: 5m,
                            MunicipalSpendAmount: 2m)
                    ],
                    CorrelationId: "corr:cashflow",
                    OccurredAtUtc: occurredAtUtc));
            await dbContext.SaveChangesAsync();

            var messages = dbContext.OutboxMessages.OrderBy(x => x.Type)
               .ToList();

            Assert.Equal(
                expected: 4,
                actual: messages.Count);
            Assert.Contains(
                collection: messages,
                filter: x => x.Type == ClassicCityOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1);
            Assert.Contains(
                collection: messages,
                filter: x => x.Type == ClassicCityOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1);
            Assert.Contains(
                collection: messages,
                filter: x => x.Type == ClassicCityOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1);
            Assert.Contains(
                collection: messages,
                filter: x => x.Type == ClassicCityOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1);
            Assert.All(
                collection: messages,
                action: x => Assert.Equal(
                    expected: occurredAtUtc.UtcDateTime,
                    actual: x.OccurredOnUtc));
        }
    }
}
