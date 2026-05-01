using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHouseholdConsumerSpendAllocationPolicyTests
{
    [Fact]
    public void Allocate_WhenRetailTurnoverIsNotPositive_ReturnsEmpty()
    {
        var policy = new CityHouseholdConsumerSpendAllocationPolicy();

        IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
            householdId: Guid.NewGuid(),
            currentDate: new DateOnly(2048, 5, 5),
            retailTurnover: Money.Zero,
            totalSalesTax: Money.Zero,
            retailStoreSpend: Money.Zero,
            serviceSpend: Money.Zero,
            municipalSpend: Money.Zero,
            businesses:
            [
                EconomyTestData.CreateBusiness(Guid.NewGuid(), CityBusinessKind.RetailStore, "Retail")
            ]);

        Assert.Empty(allocations);
    }

    [Fact]
    public void Allocate_WhenCategorizedSpendIsMissing_FallsBackToSingleRetailSegment()
    {
        Guid cityId = Guid.NewGuid();
        CityBusiness retail = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail");
        var policy = new CityHouseholdConsumerSpendAllocationPolicy();

        IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
            householdId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            currentDate: new DateOnly(2048, 5, 5),
            retailTurnover: Money.FromDecimal(120m),
            totalSalesTax: Money.FromDecimal(12m),
            retailStoreSpend: Money.Zero,
            serviceSpend: Money.Zero,
            municipalSpend: Money.Zero,
            businesses: [retail]);

        CityHouseholdConsumerSpendAllocation allocation = Assert.Single(allocations);
        Assert.Equal(retail.Id, allocation.Business.Id);
        Assert.Equal("retail-store", allocation.SegmentKey);
        Assert.Equal(Money.FromDecimal(120m), allocation.GrossAmount);
        Assert.Equal(Money.FromDecimal(12m), allocation.SalesTaxAmount);
    }

    [Fact]
    public void Allocate_WhenSegmentsAreProvided_DistributesTaxAndUsesPreferredBusinessKinds()
    {
        Guid cityId = Guid.NewGuid();
        CityBusiness retail = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail");
        CityBusiness service = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service");
        CityBusiness municipal = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Municipal");
        var policy = new CityHouseholdConsumerSpendAllocationPolicy();

        IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations = policy.Allocate(
            householdId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            currentDate: new DateOnly(2048, 5, 6),
            retailTurnover: Money.FromDecimal(100m),
            totalSalesTax: Money.FromDecimal(10m),
            retailStoreSpend: Money.FromDecimal(60m),
            serviceSpend: Money.FromDecimal(30m),
            municipalSpend: Money.FromDecimal(10m),
            businesses: [municipal, service, retail]);

        Assert.Collection(
            allocations,
            retailAllocation =>
            {
                Assert.Equal(retail.Id, retailAllocation.Business.Id);
                Assert.Equal("retail-store", retailAllocation.SegmentKey);
                Assert.Equal(Money.FromDecimal(60m), retailAllocation.GrossAmount);
                Assert.Equal(Money.FromDecimal(6m), retailAllocation.SalesTaxAmount);
            },
            serviceAllocation =>
            {
                Assert.Equal(service.Id, serviceAllocation.Business.Id);
                Assert.Equal("service", serviceAllocation.SegmentKey);
                Assert.Equal(Money.FromDecimal(30m), serviceAllocation.GrossAmount);
                Assert.Equal(Money.FromDecimal(3m), serviceAllocation.SalesTaxAmount);
            },
            municipalAllocation =>
            {
                Assert.Equal(municipal.Id, municipalAllocation.Business.Id);
                Assert.Equal("municipal", municipalAllocation.SegmentKey);
                Assert.Equal(Money.FromDecimal(10m), municipalAllocation.GrossAmount);
                Assert.Equal(Money.FromDecimal(1m), municipalAllocation.SalesTaxAmount);
            });
    }

    [Fact]
    public void Allocate_WhenCalledWithSameHouseholdAndDate_SelectsBusinessesDeterministically()
    {
        Guid cityId = Guid.NewGuid();
        IReadOnlyList<CityBusiness> businesses =
        [
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail A"),
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail B"),
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service A"),
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service B"),
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor A"),
            EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor B")
        ];
        var policy = new CityHouseholdConsumerSpendAllocationPolicy();

        IReadOnlyList<CityHouseholdConsumerSpendAllocation> firstRun = policy.Allocate(
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            currentDate: new DateOnly(2048, 5, 7),
            retailTurnover: Money.FromDecimal(90m),
            totalSalesTax: Money.FromDecimal(9m),
            retailStoreSpend: Money.FromDecimal(40m),
            serviceSpend: Money.FromDecimal(30m),
            municipalSpend: Money.FromDecimal(20m),
            businesses: businesses);
        IReadOnlyList<CityHouseholdConsumerSpendAllocation> secondRun = policy.Allocate(
            householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            currentDate: new DateOnly(2048, 5, 7),
            retailTurnover: Money.FromDecimal(90m),
            totalSalesTax: Money.FromDecimal(9m),
            retailStoreSpend: Money.FromDecimal(40m),
            serviceSpend: Money.FromDecimal(30m),
            municipalSpend: Money.FromDecimal(20m),
            businesses: businesses);

        Assert.Equal(firstRun.Select(x => x.Business.Id), secondRun.Select(x => x.Business.Id));
        Assert.Equal(firstRun.Select(x => x.SegmentKey), secondRun.Select(x => x.SegmentKey));
    }
}
