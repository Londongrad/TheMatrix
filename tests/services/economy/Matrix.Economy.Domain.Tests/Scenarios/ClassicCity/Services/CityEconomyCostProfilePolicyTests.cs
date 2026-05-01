using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEconomyCostProfilePolicyTests
{
    [Fact]
    public void CreateSeed_WhenSimulationKindIsNotClassicCity_ReturnsNeutralSnapshot()
    {
        DateTimeOffset asOfUtc = new(2048, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: "metro",
            economyProfile: "struggling",
            asOfUtc: asOfUtc);

        Assert.Equal(1m, snapshot.WageMultiplier);
        Assert.Equal(1m, snapshot.RetailPriceMultiplier);
        Assert.Equal(1m, snapshot.HousingCostMultiplier);
        Assert.Equal(1m, snapshot.UtilityCostMultiplier);
        Assert.Equal(1m, snapshot.CostOfLivingIndex);
        Assert.Equal(1m, snapshot.AffordabilityIndex);
        Assert.Equal(asOfUtc, snapshot.EvaluatedAtUtc);
    }

    [Fact]
    public void CreateSeed_WhenEconomyProfileIsStruggling_ReturnsStrugglingPreset()
    {
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: " classiccity ",
            economyProfile: " struggling ",
            asOfUtc: new DateTimeOffset(2048, 3, 4, 5, 6, 7, TimeSpan.Zero));

        Assert.Equal(0.86m, snapshot.WageMultiplier);
        Assert.Equal(0.94m, snapshot.RetailPriceMultiplier);
        Assert.Equal(0.97m, snapshot.HousingCostMultiplier);
        Assert.Equal(0.98m, snapshot.UtilityCostMultiplier);
        Assert.Equal(0.9633m, snapshot.CostOfLivingIndex);
        Assert.Equal(0.8928m, snapshot.AffordabilityIndex);
    }

    [Fact]
    public void CreateSeed_WhenEconomyProfileIsAffluent_ReturnsAffluentPreset()
    {
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
            simulationKind: "CLASSICCITY",
            economyProfile: "AFFLUENT",
            asOfUtc: new DateTimeOffset(2048, 3, 4, 5, 6, 7, TimeSpan.Zero));

        Assert.Equal(1.18m, snapshot.WageMultiplier);
        Assert.Equal(1.08m, snapshot.RetailPriceMultiplier);
        Assert.Equal(1.14m, snapshot.HousingCostMultiplier);
        Assert.Equal(1.07m, snapshot.UtilityCostMultiplier);
        Assert.Equal(1.0967m, snapshot.CostOfLivingIndex);
        Assert.Equal(1.0760m, snapshot.AffordabilityIndex);
    }

    [Fact]
    public void Recalculate_WhenBudgetAndBusinessSupportAreStrong_ImprovesAffordabilityAgainstStressedScenario()
    {
        Guid cityId = Guid.NewGuid();
        CityEconomyCostProfileState strongState = CreateNeutralState(cityId);
        CityEconomyCostProfileState stressedState = CreateNeutralState(cityId);
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot strongSnapshot = policy.Recalculate(
            state: strongState,
            budget: EconomyTestData.CreateBudget(cityId, balance: 50_000m),
            allocations:
            [
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Housing, targetAmount: 1_000m, totalSpent: 200m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.General, targetAmount: 1_000m, totalSpent: 250m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Commerce, targetAmount: 900m, totalSpent: 150m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Infrastructure, targetAmount: 1_100m, totalSpent: 200m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Operations, targetAmount: 1_000m, totalSpent: 250m)
            ],
            businesses: CreateHealthyBusinesses(cityId),
            asOfUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero));
        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot stressedSnapshot = policy.Recalculate(
            state: stressedState,
            budget: EconomyTestData.CreateBudget(cityId, balance: -100_000m),
            allocations:
            [
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Housing, targetAmount: 1_000m, totalSpent: 1_400m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.General, targetAmount: 1_000m, totalSpent: 1_300m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Commerce, targetAmount: 900m, totalSpent: 1_100m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Infrastructure, targetAmount: 1_100m, totalSpent: 1_450m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Operations, targetAmount: 1_000m, totalSpent: 1_350m)
            ],
            businesses: CreateStressedBusinesses(cityId),
            asOfUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.True(strongSnapshot.WageMultiplier > stressedSnapshot.WageMultiplier);
        Assert.True(strongSnapshot.RetailPriceMultiplier < stressedSnapshot.RetailPriceMultiplier);
        Assert.True(strongSnapshot.HousingCostMultiplier < stressedSnapshot.HousingCostMultiplier);
        Assert.True(strongSnapshot.UtilityCostMultiplier < stressedSnapshot.UtilityCostMultiplier);
        Assert.True(strongSnapshot.CostOfLivingIndex < stressedSnapshot.CostOfLivingIndex);
        Assert.True(strongSnapshot.AffordabilityIndex > stressedSnapshot.AffordabilityIndex);
    }

    [Fact]
    public void Recalculate_WhenOptionalInputsAreMissing_UsesFallbackSupportValuesAndPreservesTimestamp()
    {
        Guid cityId = Guid.NewGuid();
        CityEconomyCostProfileState state = CreateNeutralState(cityId);
        DateTimeOffset asOfUtc = new(2048, 5, 2, 0, 0, 0, TimeSpan.Zero);
        var policy = new CityEconomyCostProfilePolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot snapshot = policy.Recalculate(
            state: state,
            budget: null,
            allocations: [],
            businesses: [],
            asOfUtc: asOfUtc);

        Assert.InRange(snapshot.WageMultiplier, 0.75m, 1.35m);
        Assert.InRange(snapshot.RetailPriceMultiplier, 0.75m, 1.45m);
        Assert.InRange(snapshot.HousingCostMultiplier, 0.75m, 1.50m);
        Assert.InRange(snapshot.UtilityCostMultiplier, 0.75m, 1.45m);
        Assert.InRange(snapshot.CostOfLivingIndex, 0.20m, 3m);
        Assert.InRange(snapshot.AffordabilityIndex, 0.20m, 3m);
        Assert.Equal(asOfUtc, snapshot.EvaluatedAtUtc);
    }

    private static CityEconomyCostProfileState CreateNeutralState(Guid cityId)
    {
        return CityEconomyCostProfileState.Create(
            cityId: cityId,
            seed: new Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyCostProfileSnapshot(
                WageMultiplier: 1m,
                RetailPriceMultiplier: 1m,
                HousingCostMultiplier: 1m,
                UtilityCostMultiplier: 1m,
                CostOfLivingIndex: 1m,
                AffordabilityIndex: 1m,
                EvaluatedAtUtc: new DateTimeOffset(2048, 4, 1, 0, 0, 0, TimeSpan.Zero)),
            updatedAtUtc: new DateTimeOffset(2048, 4, 1, 0, 0, 0, TimeSpan.Zero));
    }

    private static IReadOnlyCollection<Matrix.Economy.Domain.Aggregates.CityBusiness> CreateHealthyBusinesses(Guid cityId)
    {
        Matrix.Economy.Domain.Aggregates.CityBusiness landlord = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Landlord, "Landlord", 2_000m);
        landlord.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness utility = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Utility, "Utility", 2_000m);
        utility.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness retail = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail", 2_000m);
        retail.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness employer = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Employer, "Employer", 2_000m);
        employer.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness service = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service", 2_000m);
        service.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness manufacturer = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Manufacturer, "Manufacturer", 2_000m);
        manufacturer.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));
        Matrix.Economy.Domain.Aggregates.CityBusiness municipalVendor = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor", 2_000m);
        municipalVendor.InjectCapital(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(1_000m));

        return [landlord, utility, retail, employer, service, manufacturer, municipalVendor];
    }

    private static IReadOnlyCollection<Matrix.Economy.Domain.Aggregates.CityBusiness> CreateStressedBusinesses(Guid cityId)
    {
        Matrix.Economy.Domain.Aggregates.CityBusiness landlord = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Landlord, "Landlord", 100m);
        landlord.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(180m));
        Matrix.Economy.Domain.Aggregates.CityBusiness utility = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Utility, "Utility", 100m);
        utility.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(170m));
        Matrix.Economy.Domain.Aggregates.CityBusiness retail = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.RetailStore, "Retail", 100m);
        retail.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(160m));
        Matrix.Economy.Domain.Aggregates.CityBusiness employer = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Employer, "Employer", 100m);
        employer.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(185m));
        Matrix.Economy.Domain.Aggregates.CityBusiness service = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service", 100m);
        service.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(175m));
        Matrix.Economy.Domain.Aggregates.CityBusiness manufacturer = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Manufacturer, "Manufacturer", 100m);
        manufacturer.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(165m));
        Matrix.Economy.Domain.Aggregates.CityBusiness municipalVendor = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor", 100m);
        municipalVendor.RecordOperatingExpense(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(190m));

        return [landlord, utility, retail, employer, service, manufacturer, municipalVendor];
    }
}
