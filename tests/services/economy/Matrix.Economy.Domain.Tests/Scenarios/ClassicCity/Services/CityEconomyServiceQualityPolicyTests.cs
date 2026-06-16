using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomyServiceQualityPolicyTests
    {
        [Fact]
        public void Evaluate_WhenFundingAndBusinessSupportAreStrong_OutperformsStressedScenario()
        {
            var cityId = Guid.NewGuid();
            var policy = new CityEconomyServiceQualityPolicy();

            CityEconomyServiceQualitySnapshot strongSnapshot = policy.Evaluate(
                budget: EconomyTestData.CreateBudget(
                    cityId: cityId,
                    balance: 40_000m),
                allocations:
                [
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Healthcare,
                        targetAmount: 1_200m,
                        totalSpent: 250m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Education,
                        targetAmount: 1_100m,
                        totalSpent: 220m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Housing,
                        targetAmount: 1_300m,
                        totalSpent: 260m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Operations,
                        targetAmount: 1_000m,
                        totalSpent: 250m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.General,
                        targetAmount: 1_000m,
                        totalSpent: 240m)
                ],
                businesses: CreateHealthyBusinesses(cityId),
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            CityEconomyServiceQualitySnapshot stressedSnapshot = policy.Evaluate(
                budget: EconomyTestData.CreateBudget(
                    cityId: cityId,
                    balance: -90_000m),
                allocations:
                [
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Healthcare,
                        targetAmount: 1_200m,
                        totalSpent: 1_600m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Education,
                        targetAmount: 1_100m,
                        totalSpent: 1_500m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Housing,
                        targetAmount: 1_300m,
                        totalSpent: 1_700m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Operations,
                        targetAmount: 1_000m,
                        totalSpent: 1_450m),
                    EconomyTestData.CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.General,
                        targetAmount: 1_000m,
                        totalSpent: 1_400m)
                ],
                businesses: CreateStressedBusinesses(cityId),
                asOfUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            Assert.True(strongSnapshot.HealthcareQualityIndex > stressedSnapshot.HealthcareQualityIndex);
            Assert.True(strongSnapshot.EducationQualityIndex > stressedSnapshot.EducationQualityIndex);
            Assert.True(strongSnapshot.HousingSupportIndex > stressedSnapshot.HousingSupportIndex);
        }

        [Fact]
        public void Evaluate_WhenOptionalInputsAreMissing_UsesFallbackValuesAndPreservesTimestamp()
        {
            DateTimeOffset asOfUtc = new(
                year: 2048,
                month: 5,
                day: 4,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var policy = new CityEconomyServiceQualityPolicy();

            CityEconomyServiceQualitySnapshot snapshot = policy.Evaluate(
                budget: null,
                allocations: [],
                businesses: [],
                asOfUtc: asOfUtc);

            Assert.InRange(
                actual: snapshot.HealthcareQualityIndex,
                low: 0.45m,
                high: 1.55m);
            Assert.InRange(
                actual: snapshot.EducationQualityIndex,
                low: 0.45m,
                high: 1.55m);
            Assert.InRange(
                actual: snapshot.HousingSupportIndex,
                low: 0.45m,
                high: 1.60m);
            Assert.Equal(
                expected: asOfUtc,
                actual: snapshot.EvaluatedAtUtc);
        }

        private static IReadOnlyCollection<CityBusiness> CreateHealthyBusinesses(Guid cityId)
        {
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Service",
                initialCapital: 2_000m);
            service.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness landlord = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Landlord,
                name: "Landlord",
                initialCapital: 2_000m);
            landlord.InjectCapital(Money.FromDecimal(1_000m));
            CityBusiness municipalVendor = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Vendor",
                initialCapital: 2_000m);
            municipalVendor.InjectCapital(Money.FromDecimal(1_000m));

            return
            [
                service,
                landlord,
                municipalVendor
            ];
        }

        private static IReadOnlyCollection<CityBusiness> CreateStressedBusinesses(Guid cityId)
        {
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Service",
                initialCapital: 100m);
            service.RecordOperatingExpense(Money.FromDecimal(180m));
            CityBusiness landlord = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Landlord,
                name: "Landlord",
                initialCapital: 100m);
            landlord.RecordOperatingExpense(Money.FromDecimal(170m));
            CityBusiness municipalVendor = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Vendor",
                initialCapital: 100m);
            municipalVendor.RecordOperatingExpense(Money.FromDecimal(190m));

            return
            [
                service,
                landlord,
                municipalVendor
            ];
        }
    }
}
