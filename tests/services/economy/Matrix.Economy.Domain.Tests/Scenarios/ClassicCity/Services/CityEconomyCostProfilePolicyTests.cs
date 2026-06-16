using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomyCostProfilePolicyTests
    {
        [Fact]
        public void CreateSeed_WhenScenarioKeyIsNotClassicCity_ReturnsNeutralSnapshot()
        {
            DateTimeOffset asOfUtc = new(
                year: 2048,
                month: 3,
                day: 4,
                hour: 5,
                minute: 6,
                second: 7,
                offset: TimeSpan.Zero);
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
                scenarioKey: "metro",
                economyProfile: "struggling",
                asOfUtc: asOfUtc);

            Assert.Equal(
                expected: 1m,
                actual: snapshot.WageMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.HousingCostMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.CostOfLivingIndex);
            Assert.Equal(
                expected: 1m,
                actual: snapshot.AffordabilityIndex);
            Assert.Equal(
                expected: asOfUtc,
                actual: snapshot.EvaluatedAtUtc);
        }

        [Fact]
        public void CreateSeed_WhenEconomyProfileIsStruggling_ReturnsStrugglingPreset()
        {
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
                scenarioKey: " classiccity ",
                economyProfile: " struggling ",
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 3,
                    day: 4,
                    hour: 5,
                    minute: 6,
                    second: 7,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 0.86m,
                actual: snapshot.WageMultiplier);
            Assert.Equal(
                expected: 0.94m,
                actual: snapshot.RetailPriceMultiplier);
            Assert.Equal(
                expected: 0.97m,
                actual: snapshot.HousingCostMultiplier);
            Assert.Equal(
                expected: 0.98m,
                actual: snapshot.UtilityCostMultiplier);
            Assert.Equal(
                expected: 0.9633m,
                actual: snapshot.CostOfLivingIndex);
            Assert.Equal(
                expected: 0.8928m,
                actual: snapshot.AffordabilityIndex);
        }

        [Fact]
        public void CreateSeed_WhenCanonicalClassicCityKeyIsUsed_ReturnsRequestedPreset()
        {
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
                scenarioKey: "classic-city",
                economyProfile: "struggling",
                asOfUtc: EconomyTestData.DefaultCreatedAtUtc);

            Assert.Equal(
                expected: 0.86m,
                actual: snapshot.WageMultiplier);
        }

        [Fact]
        public void CreateSeed_WhenEconomyProfileIsAffluent_ReturnsAffluentPreset()
        {
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot snapshot = policy.CreateSeed(
                scenarioKey: "CLASSICCITY",
                economyProfile: "AFFLUENT",
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 3,
                    day: 4,
                    hour: 5,
                    minute: 6,
                    second: 7,
                    offset: TimeSpan.Zero));

            Assert.Equal(
                expected: 1.18m,
                actual: snapshot.WageMultiplier);
            Assert.Equal(
                expected: 1.08m,
                actual: snapshot.RetailPriceMultiplier);
            Assert.Equal(
                expected: 1.14m,
                actual: snapshot.HousingCostMultiplier);
            Assert.Equal(
                expected: 1.07m,
                actual: snapshot.UtilityCostMultiplier);
            Assert.Equal(
                expected: 1.0967m,
                actual: snapshot.CostOfLivingIndex);
            Assert.Equal(
                expected: 1.0760m,
                actual: snapshot.AffordabilityIndex);
        }

        [Fact]
        public void Recalculate_WhenBudgetAndBusinessSupportAreStrong_ImprovesAffordabilityAgainstStressedScenario()
        {
            var cityId = Guid.NewGuid();
            CityEconomyCostProfileState strongState = CreateNeutralState(cityId);
            CityEconomyCostProfileState stressedState = CreateNeutralState(cityId);
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot strongSnapshot = policy.Recalculate(
                state: strongState,
                budget: EconomyTestData.CreateBudget(
                    cityId: cityId,
                    balance: 50_000m),
                allocations:
                [
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Housing,
                        targetAmount: 1_000m,
                        totalSpent: 200m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.General,
                        targetAmount: 1_000m,
                        totalSpent: 250m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Commerce,
                        targetAmount: 900m,
                        totalSpent: 150m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Infrastructure,
                        targetAmount: 1_100m,
                        totalSpent: 200m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Operations,
                        targetAmount: 1_000m,
                        totalSpent: 250m)
                ],
                businesses: CreateHealthyBusinesses(cityId),
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityEconomyCostProfileSnapshot stressedSnapshot = policy.Recalculate(
                state: stressedState,
                budget: EconomyTestData.CreateBudget(
                    cityId: cityId,
                    balance: -100_000m),
                allocations:
                [
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Housing,
                        targetAmount: 1_000m,
                        totalSpent: 1_400m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.General,
                        targetAmount: 1_000m,
                        totalSpent: 1_300m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Commerce,
                        targetAmount: 900m,
                        totalSpent: 1_100m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Infrastructure,
                        targetAmount: 1_100m,
                        totalSpent: 1_450m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Operations,
                        targetAmount: 1_000m,
                        totalSpent: 1_350m)
                ],
                businesses: CreateStressedBusinesses(cityId),
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

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
            var cityId = Guid.NewGuid();
            CityEconomyCostProfileState state = CreateNeutralState(cityId);
            DateTimeOffset asOfUtc = new(
                year: 2048,
                month: 5,
                day: 2,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var policy = new CityEconomyCostProfilePolicy();

            CityEconomyCostProfileSnapshot snapshot = policy.Recalculate(
                state: state,
                budget: null,
                allocations: [],
                businesses: [],
                asOfUtc: asOfUtc);

            Assert.InRange(
                actual: snapshot.WageMultiplier,
                low: 0.75m,
                high: 1.35m);
            Assert.InRange(
                actual: snapshot.RetailPriceMultiplier,
                low: 0.75m,
                high: 1.45m);
            Assert.InRange(
                actual: snapshot.HousingCostMultiplier,
                low: 0.75m,
                high: 1.50m);
            Assert.InRange(
                actual: snapshot.UtilityCostMultiplier,
                low: 0.75m,
                high: 1.45m);
            Assert.InRange(
                actual: snapshot.CostOfLivingIndex,
                low: 0.20m,
                high: 3m);
            Assert.InRange(
                actual: snapshot.AffordabilityIndex,
                low: 0.20m,
                high: 3m);
            Assert.Equal(
                expected: asOfUtc,
                actual: snapshot.EvaluatedAtUtc);
        }

        private static CityEconomyCostProfileState CreateNeutralState(Guid cityId)
        {
            return CityEconomyCostProfileState.Create(
                cityId: cityId,
                seed: new CityEconomyCostProfileSnapshot(
                    WageMultiplier: 1m,
                    RetailPriceMultiplier: 1m,
                    HousingCostMultiplier: 1m,
                    UtilityCostMultiplier: 1m,
                    CostOfLivingIndex: 1m,
                    AffordabilityIndex: 1m,
                    EvaluatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 4,
                        day: 1,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 4,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private static IReadOnlyCollection<CityBusiness> CreateHealthyBusinesses(Guid cityId)
        {
            CityBusiness landlord = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Landlord,
                name: "Landlord",
                initialCapital: 2_000m);
            landlord.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness utility = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Utility,
                name: "Utility",
                initialCapital: 2_000m);
            utility.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness retail = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.RetailStore,
                name: "Retail",
                initialCapital: 2_000m);
            retail.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness employer = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Employer,
                name: "Employer",
                initialCapital: 2_000m);
            employer.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Service",
                initialCapital: 2_000m);
            service.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness manufacturer = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Manufacturer,
                name: "Manufacturer",
                initialCapital: 2_000m);
            manufacturer.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness municipalVendor = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Vendor",
                initialCapital: 2_000m);
            municipalVendor.InjectCapital(Money.FromDecimal(1_000m));

            return
            [
                landlord,
                utility,
                retail,
                employer,
                service,
                manufacturer,
                municipalVendor
            ];
        }

        private static IReadOnlyCollection<CityBusiness> CreateStressedBusinesses(Guid cityId)
        {
            CityBusiness landlord = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Landlord,
                name: "Landlord",
                initialCapital: 100m);
            landlord.RecordOperatingExpense(Money.FromDecimal(180m));
            CityBusiness utility = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Utility,
                name: "Utility",
                initialCapital: 100m);
            utility.RecordOperatingExpense(Money.FromDecimal(170m));
            CityBusiness retail = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.RetailStore,
                name: "Retail",
                initialCapital: 100m);
            retail.RecordOperatingExpense(Money.FromDecimal(160m));
            CityBusiness employer = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Employer,
                name: "Employer",
                initialCapital: 100m);
            employer.RecordOperatingExpense(Money.FromDecimal(185m));
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Service",
                initialCapital: 100m);
            service.RecordOperatingExpense(Money.FromDecimal(175m));
            CityBusiness manufacturer = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Manufacturer,
                name: "Manufacturer",
                initialCapital: 100m);
            manufacturer.RecordOperatingExpense(Money.FromDecimal(165m));
            CityBusiness municipalVendor = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Vendor",
                initialCapital: 100m);
            municipalVendor.RecordOperatingExpense(Money.FromDecimal(190m));

            return
            [
                landlord,
                utility,
                retail,
                employer,
                service,
                manufacturer,
                municipalVendor
            ];
        }
    }
}
