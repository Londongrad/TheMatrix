using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Outbox;

public sealed class CityPopulationSignalOutboxWriterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishClassicCityCostOfLivingSnapshotAsync_AddsOutboxMessage()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var snapshot = new ClassicCityCostOfLivingSnapshotV1(
            CityId: cityId,
            WageMultiplier: 1.1m,
            RetailPriceMultiplier: 1.2m,
            HousingCostMultiplier: 1.3m,
            UtilityCostMultiplier: 1.4m,
            CostOfLivingIndex: 1.25m,
            AffordabilityIndex: 0.92m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero));

        await using var dbContext = CreateDbContext();
        var writer = new CityPopulationSignalOutboxWriter(dbContext);

        await writer.PublishClassicCityCostOfLivingSnapshotAsync(snapshot);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        var payload = JsonSerializer.Deserialize<ClassicCityCostOfLivingSnapshotV1>(message.PayloadJson, JsonOptions);
        Assert.Equal(EconomyOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1, message.Type);
        Assert.NotNull(payload);
        Assert.Equal(cityId, payload.CityId);
        Assert.Equal(1.25m, payload.CostOfLivingIndex);
    }

    [Fact]
    public async Task PublishClassicCityServiceQualitySnapshotAsync_AddsOutboxMessage()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var snapshot = new ClassicCityServiceQualitySnapshotV1(
            CityId: cityId,
            HealthcareQualityIndex: 0.8m,
            EducationQualityIndex: 0.7m,
            HousingSupportIndex: 0.6m,
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 12, 30, 0, TimeSpan.Zero));

        await using var dbContext = CreateDbContext();
        var writer = new CityPopulationSignalOutboxWriter(dbContext);

        await writer.PublishClassicCityServiceQualitySnapshotAsync(snapshot);
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(dbContext.OutboxMessages);
        var payload = JsonSerializer.Deserialize<ClassicCityServiceQualitySnapshotV1>(message.PayloadJson, JsonOptions);
        Assert.Equal(EconomyOutboxEventTypes.ClassicCityServiceQualitySnapshotV1, message.Type);
        Assert.NotNull(payload);
        Assert.Equal(0.8m, payload.HealthcareQualityIndex);
    }

    [Fact]
    public async Task PublishFinancialStressBatches_AddsTwoTypedMessages()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employerBatch = new ClassicCityEmployerFinancialStressBatchV1(
            CityId: cityId,
            BatchNumber: 1,
            TotalBatches: 2,
            Employers:
            [
                new ClassicCityEmployerFinancialStressItemV1(
                    EmployerBusinessId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    WorkplaceExternalReferenceCode: "work-1",
                    RequestedGrossPayrollAmount: 100m,
                    PaidGrossPayrollAmount: 80m,
                    MissedGrossPayrollAmount: 20m,
                    PayrollFulfillmentRatio: 0.8m,
                    FailedPayrollCount: 1,
                    PartialPayrollCount: 0,
                    CurrentBalanceAmount: 50m,
                    DistressScore: 0.7m,
                    HasHiringFreeze: true,
                    HasLayoffPressure: false)
            ],
            CorrelationId: "corr-1",
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero));
        var householdBatch = new ClassicCityHouseholdFinancialStressBatchV1(
            CityId: cityId,
            BatchNumber: 1,
            TotalBatches: 1,
            Households:
            [
                new ClassicCityHouseholdFinancialStressItemV1(
                    HouseholdAccountId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    HouseholdExternalReferenceCode: "hh-1",
                    OverdueObligationCount: 2,
                    OverdueRentCount: 1,
                    OverdueUtilityCount: 1,
                    ArrearsObligationCount: 1,
                    ServiceCutoffCount: 0,
                    EvictionNoticeCount: 0,
                    EvictionEligibleCount: 0,
                    OldestOverdueAgeDays: 15,
                    TotalOverdueAmount: 120m,
                    DistressScore: 0.55m)
            ],
            CorrelationId: "corr-2",
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 13, 10, 0, TimeSpan.Zero));

        await using var dbContext = CreateDbContext();
        var writer = new CityPopulationSignalOutboxWriter(dbContext);

        await writer.PublishClassicCityEmployerFinancialStressBatchAsync(employerBatch);
        await writer.PublishClassicCityHouseholdFinancialStressBatchAsync(householdBatch);
        await dbContext.SaveChangesAsync();

        Assert.Collection(
            dbContext.OutboxMessages.OrderBy(x => x.OccurredOnUtc),
            x => Assert.Equal(EconomyOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1, x.Type),
            x => Assert.Equal(EconomyOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1, x.Type));
    }
}
