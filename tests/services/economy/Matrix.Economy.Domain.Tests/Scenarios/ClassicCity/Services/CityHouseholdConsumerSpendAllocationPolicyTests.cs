using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdConsumerSpendAllocationPolicyTests
    {
        [Fact]
        public void Allocate_WhenRetailTurnoverIsNotPositive_ReturnsEmpty()
        {
            var policy = new CityHouseholdConsumerSpendAllocationPolicy();

            IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
                householdId: Guid.NewGuid(),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 5),
                retailTurnover: Money.Zero,
                totalSalesTax: Money.Zero,
                retailStoreSpend: Money.Zero,
                serviceSpend: Money.Zero,
                municipalSpend: Money.Zero,
                businesses:
                [
                    EconomyTestData.CreateBusiness(
                        cityId: Guid.NewGuid(),
                        kind: CityBusinessKind.RetailStore,
                        name: "Retail")
                ]);

            Assert.Empty(allocations);
        }

        [Fact]
        public void Allocate_WhenCategorizedSpendIsMissing_FallsBackToSingleRetailSegment()
        {
            var cityId = Guid.NewGuid();
            CityBusiness retail = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.RetailStore,
                name: "Retail");
            var policy = new CityHouseholdConsumerSpendAllocationPolicy();

            IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
                householdId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 5),
                retailTurnover: Money.FromDecimal(120m),
                totalSalesTax: Money.FromDecimal(12m),
                retailStoreSpend: Money.Zero,
                serviceSpend: Money.Zero,
                municipalSpend: Money.Zero,
                businesses: [retail]);

            CityHouseholdConsumerSpendAllocation allocation = Assert.Single(allocations);
            Assert.Equal(
                expected: retail.Id,
                actual: allocation.Business.Id);
            Assert.Equal(
                expected: "retail-store",
                actual: allocation.SegmentKey);
            Assert.Equal(
                expected: Money.FromDecimal(120m),
                actual: allocation.GrossAmount);
            Assert.Equal(
                expected: Money.FromDecimal(12m),
                actual: allocation.SalesTaxAmount);
        }

        [Fact]
        public void Allocate_WhenSegmentsAreProvided_DistributesTaxAndUsesPreferredBusinessKinds()
        {
            var cityId = Guid.NewGuid();
            CityBusiness retail = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.RetailStore,
                name: "Retail");
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Service");
            CityBusiness municipal = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Municipal");
            var policy = new CityHouseholdConsumerSpendAllocationPolicy();

            IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
                householdId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                retailTurnover: Money.FromDecimal(100m),
                totalSalesTax: Money.FromDecimal(10m),
                retailStoreSpend: Money.FromDecimal(60m),
                serviceSpend: Money.FromDecimal(30m),
                municipalSpend: Money.FromDecimal(10m),
                businesses:
                [
                    municipal,
                    service,
                    retail
                ]);

            Assert.Collection(
                collection: allocations,
                retailAllocation =>
                {
                    Assert.Equal(
                        expected: retail.Id,
                        actual: retailAllocation.Business.Id);
                    Assert.Equal(
                        expected: "retail-store",
                        actual: retailAllocation.SegmentKey);
                    Assert.Equal(
                        expected: Money.FromDecimal(60m),
                        actual: retailAllocation.GrossAmount);
                    Assert.Equal(
                        expected: Money.FromDecimal(6m),
                        actual: retailAllocation.SalesTaxAmount);
                },
                serviceAllocation =>
                {
                    Assert.Equal(
                        expected: service.Id,
                        actual: serviceAllocation.Business.Id);
                    Assert.Equal(
                        expected: "service",
                        actual: serviceAllocation.SegmentKey);
                    Assert.Equal(
                        expected: Money.FromDecimal(30m),
                        actual: serviceAllocation.GrossAmount);
                    Assert.Equal(
                        expected: Money.FromDecimal(3m),
                        actual: serviceAllocation.SalesTaxAmount);
                },
                municipalAllocation =>
                {
                    Assert.Equal(
                        expected: municipal.Id,
                        actual: municipalAllocation.Business.Id);
                    Assert.Equal(
                        expected: "municipal",
                        actual: municipalAllocation.SegmentKey);
                    Assert.Equal(
                        expected: Money.FromDecimal(10m),
                        actual: municipalAllocation.GrossAmount);
                    Assert.Equal(
                        expected: Money.FromDecimal(1m),
                        actual: municipalAllocation.SalesTaxAmount);
                });
        }

        [Fact]
        public void Allocate_WhenCalledWithSameHouseholdAndDate_SelectsBusinessesDeterministically()
        {
            var cityId = Guid.NewGuid();
            IReadOnlyList<CityBusiness> businesses =
            [
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.RetailStore,
                    name: "Retail A"),
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.RetailStore,
                    name: "Retail B"),
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.Service,
                    name: "Service A"),
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.Service,
                    name: "Service B"),
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.MunicipalVendor,
                    name: "Vendor A"),
                EconomyTestData.CreateBusiness(
                    cityId: cityId,
                    kind: CityBusinessKind.MunicipalVendor,
                    name: "Vendor B")
            ];
            var policy = new CityHouseholdConsumerSpendAllocationPolicy();

            IReadOnlyList<CityHouseholdConsumerSpendAllocation> firstRun = policy.Allocate(
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 7),
                retailTurnover: Money.FromDecimal(90m),
                totalSalesTax: Money.FromDecimal(9m),
                retailStoreSpend: Money.FromDecimal(40m),
                serviceSpend: Money.FromDecimal(30m),
                municipalSpend: Money.FromDecimal(20m),
                businesses: businesses);
            IReadOnlyList<CityHouseholdConsumerSpendAllocation> secondRun = policy.Allocate(
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 7),
                retailTurnover: Money.FromDecimal(90m),
                totalSalesTax: Money.FromDecimal(9m),
                retailStoreSpend: Money.FromDecimal(40m),
                serviceSpend: Money.FromDecimal(30m),
                municipalSpend: Money.FromDecimal(20m),
                businesses: businesses);

            Assert.Equal(
                expected: firstRun.Select(x => x.Business.Id),
                actual: secondRun.Select(x => x.Business.Id));
            Assert.Equal(
                expected: firstRun.Select(x => x.SegmentKey),
                actual: secondRun.Select(x => x.SegmentKey));
        }
    }
}
