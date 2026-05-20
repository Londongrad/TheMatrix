using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Common;

public sealed class ClassicCityEconomySettlementBatchFactoryTests
{
    private static readonly Guid CityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateOnly CurrentDate = new(2030, 1, 2);
    private static readonly DateTimeOffset OccurredAtUtc = new(2030, 1, 2, 12, 0, 0, TimeSpan.Zero);
    private const string CorrelationId = "classic-city:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:tick:bbbb:cashflow";

    [Fact]
    public void BuildHouseholdExternalReferenceCode_ReturnsDeterministicExpectedValue()
    {
        HouseholdId householdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        string result = ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId);

        Assert.Equal("classic-city-household:11111111111111111111111111111111", result);
    }

    [Fact]
    public void BuildHouseholdCashflowSettlementBatches_WhenItemsAreEmpty_ReturnsEmptyArray()
    {
        ClassicCityHouseholdCashflowSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 1,
                items: [],
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Empty(batches);
    }

    [Fact]
    public void BuildHouseholdCashflowSettlementBatches_WhenSettledDaysIsZeroOrNegative_ReturnsEmptyArray()
    {
        ClassicCityHouseholdCashflowSettlementItemV1[] items = [CreateHouseholdCashflowItem(1)];

        ClassicCityHouseholdCashflowSettlementBatchV1[] zeroDayBatches =
            ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 0,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);
        ClassicCityHouseholdCashflowSettlementBatchV1[] negativeDayBatches =
            ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: -1,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Empty(zeroDayBatches);
        Assert.Empty(negativeDayBatches);
    }

    [Fact]
    public void BuildHouseholdCashflowSettlementBatches_WithFiveHundredItems_CreatesOneBatch()
    {
        ClassicCityHouseholdCashflowSettlementItemV1[] items = CreateHouseholdCashflowItems(count: 500);

        ClassicCityHouseholdCashflowSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 2,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        ClassicCityHouseholdCashflowSettlementBatchV1 batch = Assert.Single(batches);

        Assert.Equal(1, batch.BatchNumber);
        Assert.Equal(1, batch.TotalBatches);
        Assert.Equal(500, batch.Households.Count);
        Assert.Equal(CityId, batch.CityId);
        Assert.Equal(CurrentDate, batch.CurrentDate);
        Assert.Equal(2, batch.SettledDays);
        Assert.Equal(CorrelationId, batch.CorrelationId);
        Assert.Equal(OccurredAtUtc, batch.OccurredAtUtc);
        AssertHouseholdOrder(
            expected: items,
            actual: batch.Households);
    }

    [Fact]
    public void BuildHouseholdCashflowSettlementBatches_WithFiveHundredOneItems_CreatesTwoBatches()
    {
        ClassicCityHouseholdCashflowSettlementItemV1[] items = CreateHouseholdCashflowItems(count: 501);

        ClassicCityHouseholdCashflowSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildHouseholdCashflowSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 2,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Equal(2, batches.Length);
        Assert.Equal(1, batches[0].BatchNumber);
        Assert.Equal(2, batches[0].TotalBatches);
        Assert.Equal(500, batches[0].Households.Count);
        Assert.Equal(2, batches[1].BatchNumber);
        Assert.Equal(2, batches[1].TotalBatches);
        Assert.Single(batches[1].Households);
        AssertHouseholdOrder(
            expected: items,
            actual: batches.SelectMany(x => x.Households)
               .ToArray());
    }

    [Fact]
    public void BuildWorkplacePayrollSettlementBatches_WhenItemsAreEmpty_ReturnsEmptyArray()
    {
        ClassicCityWorkplacePayrollSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 1,
                items: [],
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Empty(batches);
    }

    [Fact]
    public void BuildWorkplacePayrollSettlementBatches_WhenSettledDaysIsZeroOrNegative_ReturnsEmptyArray()
    {
        ClassicCityWorkplacePayrollSettlementItemV1[] items = [CreateWorkplacePayrollItem(1)];

        ClassicCityWorkplacePayrollSettlementBatchV1[] zeroDayBatches =
            ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 0,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);
        ClassicCityWorkplacePayrollSettlementBatchV1[] negativeDayBatches =
            ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: -1,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Empty(zeroDayBatches);
        Assert.Empty(negativeDayBatches);
    }

    [Fact]
    public void BuildWorkplacePayrollSettlementBatches_WithFiveHundredItems_CreatesOneBatch()
    {
        ClassicCityWorkplacePayrollSettlementItemV1[] items = CreateWorkplacePayrollItems(count: 500);

        ClassicCityWorkplacePayrollSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 2,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        ClassicCityWorkplacePayrollSettlementBatchV1 batch = Assert.Single(batches);

        Assert.Equal(1, batch.BatchNumber);
        Assert.Equal(1, batch.TotalBatches);
        Assert.Equal(500, batch.Payrolls.Count);
        Assert.Equal(CityId, batch.CityId);
        Assert.Equal(CurrentDate, batch.CurrentDate);
        Assert.Equal(2, batch.SettledDays);
        Assert.Equal(CorrelationId, batch.CorrelationId);
        Assert.Equal(OccurredAtUtc, batch.OccurredAtUtc);
        AssertPayrollOrder(
            expected: items,
            actual: batch.Payrolls);
    }

    [Fact]
    public void BuildWorkplacePayrollSettlementBatches_WithFiveHundredOneItems_CreatesTwoBatches()
    {
        ClassicCityWorkplacePayrollSettlementItemV1[] items = CreateWorkplacePayrollItems(count: 501);

        ClassicCityWorkplacePayrollSettlementBatchV1[] batches =
            ClassicCityEconomySettlementBatchFactory.BuildWorkplacePayrollSettlementBatches(
                cityId: CityId,
                currentDate: CurrentDate,
                settledDays: 2,
                items: items,
                correlationId: CorrelationId,
                occurredAtUtc: OccurredAtUtc);

        Assert.Equal(2, batches.Length);
        Assert.Equal(1, batches[0].BatchNumber);
        Assert.Equal(2, batches[0].TotalBatches);
        Assert.Equal(500, batches[0].Payrolls.Count);
        Assert.Equal(2, batches[1].BatchNumber);
        Assert.Equal(2, batches[1].TotalBatches);
        Assert.Single(batches[1].Payrolls);
        AssertPayrollOrder(
            expected: items,
            actual: batches.SelectMany(x => x.Payrolls)
               .ToArray());
    }

    private static ClassicCityHouseholdCashflowSettlementItemV1[] CreateHouseholdCashflowItems(int count)
    {
        return Enumerable.Range(
                start: 1,
                count: count)
           .Select(CreateHouseholdCashflowItem)
           .ToArray();
    }

    private static ClassicCityHouseholdCashflowSettlementItemV1 CreateHouseholdCashflowItem(int index)
    {
        Guid householdId = CreateGuid(
            prefix: "10000000",
            index: index);

        return new ClassicCityHouseholdCashflowSettlementItemV1(
            HouseholdId: householdId,
            ExternalReferenceCode: $"household-{index:D4}",
            GrossPayrollAmount: 1000m + index,
            IncomeTaxAmount: 100m + index,
            NetPayrollAmount: 900m + index,
            RetailTurnoverAmount: 50m + index,
            RetailTaxAmount: 5m + index,
            RetailStoreSpendAmount: 20m + index,
            ServiceSpendAmount: 15m + index,
            MunicipalSpendAmount: 10m + index);
    }

    private static ClassicCityWorkplacePayrollSettlementItemV1[] CreateWorkplacePayrollItems(int count)
    {
        return Enumerable.Range(
                start: 1,
                count: count)
           .Select(CreateWorkplacePayrollItem)
           .ToArray();
    }

    private static ClassicCityWorkplacePayrollSettlementItemV1 CreateWorkplacePayrollItem(int index)
    {
        Guid householdId = CreateGuid(
            prefix: "20000000",
            index: index);
        Guid workplaceId = CreateGuid(
            prefix: "30000000",
            index: index);

        return new ClassicCityWorkplacePayrollSettlementItemV1(
            HouseholdId: householdId,
            HouseholdExternalReferenceCode: $"household-{index:D4}",
            WorkplaceId: workplaceId,
            WorkplaceExternalReferenceCode: $"workplace-{index:D4}",
            JobTitle: $"Job {index:D4}",
            GrossPayrollAmount: 1000m + index,
            IncomeTaxAmount: 100m + index,
            NetPayrollAmount: 900m + index);
    }

    private static Guid CreateGuid(
        string prefix,
        int index)
    {
        return Guid.Parse($"{prefix}-0000-0000-0000-{index:000000000000}");
    }

    private static void AssertHouseholdOrder(
        IReadOnlyList<ClassicCityHouseholdCashflowSettlementItemV1> expected,
        IReadOnlyList<ClassicCityHouseholdCashflowSettlementItemV1> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(expected[i], actual[i]);
    }

    private static void AssertPayrollOrder(
        IReadOnlyList<ClassicCityWorkplacePayrollSettlementItemV1> expected,
        IReadOnlyList<ClassicCityWorkplacePayrollSettlementItemV1> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(expected[i], actual[i]);
    }
}
