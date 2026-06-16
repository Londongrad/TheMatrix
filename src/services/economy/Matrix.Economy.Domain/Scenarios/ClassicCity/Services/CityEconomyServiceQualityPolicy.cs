using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomyServiceQualityPolicy
    {
        public CityEconomyServiceQualitySnapshot Evaluate(
            CityBudget? budget,
            IReadOnlyCollection<CityBudgetAllocation> allocations,
            IReadOnlyCollection<CityBusiness> businesses,
            DateTimeOffset asOfUtc)
        {
            decimal budgetResilience = ResolveBudgetResilience(
                budget: budget,
                allocations: allocations);
            decimal healthcareFunding = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Healthcare,
                secondaryCategory: CityBudgetCategory.Operations);
            decimal educationFunding = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Education,
                secondaryCategory: CityBudgetCategory.General);
            decimal housingFunding = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Housing,
                secondaryCategory: CityBudgetCategory.General);

            decimal municipalServiceSupport = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.Service,
                CityBusinessKind.MunicipalVendor);
            decimal landlordSupport = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.Landlord,
                CityBusinessKind.MunicipalVendor);

            decimal healthcareQuality = decimal.Round(
                d: Clamp(
                    value: (healthcareFunding * 0.60m) +
                           (municipalServiceSupport * 0.25m) +
                           (budgetResilience * 0.15m),
                    min: 0.45m,
                    max: 1.55m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            decimal educationQuality = decimal.Round(
                d: Clamp(
                    value: (educationFunding * 0.65m) +
                           (municipalServiceSupport * 0.20m) +
                           (budgetResilience * 0.15m),
                    min: 0.45m,
                    max: 1.55m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            decimal housingSupport = decimal.Round(
                d: Clamp(
                    value: (housingFunding * 0.55m) +
                           (landlordSupport * 0.25m) +
                           (budgetResilience * 0.20m),
                    min: 0.45m,
                    max: 1.60m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            return new CityEconomyServiceQualitySnapshot(
                HealthcareQualityIndex: healthcareQuality,
                EducationQualityIndex: educationQuality,
                HousingSupportIndex: housingSupport,
                EvaluatedAtUtc: asOfUtc);
        }

        private static decimal ResolveAllocationSupport(
            IReadOnlyCollection<CityBudgetAllocation> allocations,
            CityBudgetCategory primaryCategory,
            CityBudgetCategory secondaryCategory)
        {
            CityBudgetAllocation? primary = allocations.FirstOrDefault(x => x.Category == primaryCategory);
            CityBudgetAllocation? secondary = allocations.FirstOrDefault(x => x.Category == secondaryCategory);

            decimal primarySupport = ResolveSingleAllocationSupport(primary);
            decimal secondarySupport = ResolveSingleAllocationSupport(secondary);

            return decimal.Round(
                d: Clamp(
                    value: primarySupport + (secondarySupport * 0.35m),
                    min: 0.25m,
                    max: 1.40m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveSingleAllocationSupport(CityBudgetAllocation? allocation)
        {
            if (allocation is null || allocation.TargetAmount.Amount <= 0m)
                return 0.72m;

            decimal spentRatio = allocation.TotalSpent.Amount / allocation.TargetAmount.Amount;
            decimal availableRatio = allocation.GetAvailableAmount()
                                        .Amount /
                                     allocation.TargetAmount.Amount;

            return Clamp(
                value: 0.76m +
                       (availableRatio * 0.32m) -
                       (Math.Max(
                            val1: 0m,
                            val2: spentRatio - 1m) *
                        0.30m),
                min: 0.20m,
                max: 1.35m);
        }

        private static decimal ResolveBudgetResilience(
            CityBudget? budget,
            IReadOnlyCollection<CityBudgetAllocation> allocations)
        {
            if (budget is null)
                return 0.80m;

            decimal reservePressure = budget.Balance.Amount >= 0m
                ? 0m
                : Clamp(
                    value: Math.Abs(budget.Balance.Amount) / 100_000m,
                    min: 0m,
                    max: 1m);
            decimal overrunPressure = allocations.Count == 0
                ? 0m
                : allocations.Average(x => Math.Max(
                                               val1: 0m,
                                               val2: x.TotalSpent.Amount - x.TargetAmount.Amount) /
                                           Math.Max(
                                               val1: 1m,
                                               val2: x.TargetAmount.Amount));

            return decimal.Round(
                d: Clamp(
                    value: 1.05m -
                           (reservePressure * 0.35m) -
                           (overrunPressure * 0.30m),
                    min: 0.45m,
                    max: 1.10m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveBusinessSupport(
            IReadOnlyCollection<CityBusiness> businesses,
            params CityBusinessKind[] allowedKinds)
        {
            IReadOnlyCollection<CityBusiness> scopedBusinesses = businesses
               .Where(x => allowedKinds.Contains(x.Kind))
               .ToArray();

            if (scopedBusinesses.Count == 0)
                return 0.78m;

            decimal support = scopedBusinesses.Average(x =>
            {
                decimal capitalBase = Math.Max(
                    val1: 1m,
                    val2: x.TotalCapitalInjections.Amount);
                decimal balanceRatio = x.Balance.Amount / capitalBase;
                decimal taxBurden = x.TaxReserve.Amount / capitalBase;

                return Clamp(
                    value: 0.78m +
                           (balanceRatio * 0.52m) -
                           (taxBurden * 0.18m),
                    min: 0.20m,
                    max: 1.40m);
            });

            return decimal.Round(
                d: support,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal Clamp(
            decimal value,
            decimal min,
            decimal max)
        {
            return Math.Min(
                val1: max,
                val2: Math.Max(
                    val1: min,
                    val2: value));
        }
    }
}
