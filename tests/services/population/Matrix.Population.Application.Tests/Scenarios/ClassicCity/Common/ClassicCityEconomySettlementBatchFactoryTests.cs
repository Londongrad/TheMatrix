using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Common
{
    public sealed class ClassicCityEconomySettlementBatchFactoryTests
    {
        private const string CorrelationId = "classic-city:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:tick:bbbb:cashflow";
        private static readonly Guid CityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly DateOnly CurrentDate = new(
            year: 2030,
            month: 1,
            day: 2);

        private static readonly DateTimeOffset OccurredAtUtc = new(
            year: 2030,
            month: 1,
            day: 2,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void BuildHouseholdExternalReferenceCode_ReturnsDeterministicExpectedValue()
        {
            var householdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

            string result = ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId);

            Assert.Equal(
                expected: "classic-city-household:11111111111111111111111111111111",
                actual: result);
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

            Assert.Equal(
                expected: 1,
                actual: batch.BatchNumber);
            Assert.Equal(
                expected: 1,
                actual: batch.TotalBatches);
            Assert.Equal(
                expected: 500,
                actual: batch.Households.Count);
            Assert.Equal(
                expected: CityId,
                actual: batch.CityId);
            Assert.Equal(
                expected: CurrentDate,
                actual: batch.CurrentDate);
            Assert.Equal(
                expected: 2,
                actual: batch.SettledDays);
            Assert.Equal(
                expected: CorrelationId,
                actual: batch.CorrelationId);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: batch.OccurredAtUtc);
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

            Assert.Equal(
                expected: 2,
                actual: batches.Length);
            Assert.Equal(
                expected: 1,
                actual: batches[0].BatchNumber);
            Assert.Equal(
                expected: 2,
                actual: batches[0].TotalBatches);
            Assert.Equal(
                expected: 500,
                actual: batches[0].Households.Count);
            Assert.Equal(
                expected: 2,
                actual: batches[1].BatchNumber);
            Assert.Equal(
                expected: 2,
                actual: batches[1].TotalBatches);
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

            Assert.Equal(
                expected: 1,
                actual: batch.BatchNumber);
            Assert.Equal(
                expected: 1,
                actual: batch.TotalBatches);
            Assert.Equal(
                expected: 500,
                actual: batch.Payrolls.Count);
            Assert.Equal(
                expected: CityId,
                actual: batch.CityId);
            Assert.Equal(
                expected: CurrentDate,
                actual: batch.CurrentDate);
            Assert.Equal(
                expected: 2,
                actual: batch.SettledDays);
            Assert.Equal(
                expected: CorrelationId,
                actual: batch.CorrelationId);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: batch.OccurredAtUtc);
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

            Assert.Equal(
                expected: 2,
                actual: batches.Length);
            Assert.Equal(
                expected: 1,
                actual: batches[0].BatchNumber);
            Assert.Equal(
                expected: 2,
                actual: batches[0].TotalBatches);
            Assert.Equal(
                expected: 500,
                actual: batches[0].Payrolls.Count);
            Assert.Equal(
                expected: 2,
                actual: batches[1].BatchNumber);
            Assert.Equal(
                expected: 2,
                actual: batches[1].TotalBatches);
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
            Assert.Equal(
                expected: expected.Count,
                actual: actual.Count);

            for (int i = 0; i < expected.Count; i++)
                Assert.Equal(
                    expected: expected[i],
                    actual: actual[i]);
        }

        private static void AssertPayrollOrder(
            IReadOnlyList<ClassicCityWorkplacePayrollSettlementItemV1> expected,
            IReadOnlyList<ClassicCityWorkplacePayrollSettlementItemV1> actual)
        {
            Assert.Equal(
                expected: expected.Count,
                actual: actual.Count);

            for (int i = 0; i < expected.Count; i++)
                Assert.Equal(
                    expected: expected[i],
                    actual: actual[i]);
        }
    }
}
