using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomyCostProfilePolicy
    {
        public CityEconomyCostProfileSnapshot CreateSeed(
            string? scenarioKey,
            string? economyProfile,
            DateTimeOffset asOfUtc)
        {
            string normalizedScenarioKey =
                scenarioKey?.Trim().ToUpperInvariant() ?? string.Empty;

            if (normalizedScenarioKey is not "CLASSICCITY" and not "CLASSIC-CITY")
                return CityEconomyCostProfileSnapshot.Neutral(asOfUtc);

            return NormalizeEconomyProfile(economyProfile) switch
            {
                "STRUGGLING" => new CityEconomyCostProfileSnapshot(
                    WageMultiplier: 0.86m,
                    RetailPriceMultiplier: 0.94m,
                    HousingCostMultiplier: 0.97m,
                    UtilityCostMultiplier: 0.98m,
                    CostOfLivingIndex: 0.9633m,
                    AffordabilityIndex: 0.8928m,
                    EvaluatedAtUtc: asOfUtc),
                "AFFLUENT" => new CityEconomyCostProfileSnapshot(
                    WageMultiplier: 1.18m,
                    RetailPriceMultiplier: 1.08m,
                    HousingCostMultiplier: 1.14m,
                    UtilityCostMultiplier: 1.07m,
                    CostOfLivingIndex: 1.0967m,
                    AffordabilityIndex: 1.0760m,
                    EvaluatedAtUtc: asOfUtc),
                _ => CityEconomyCostProfileSnapshot.Neutral(asOfUtc)
            };
        }

        public CityEconomyCostProfileSnapshot Recalculate(
            CityEconomyCostProfileState state,
            CityBudget? budget,
            IReadOnlyCollection<CityBudgetAllocation> allocations,
            IReadOnlyCollection<CityBusiness> businesses,
            DateTimeOffset asOfUtc)
        {
            decimal housingSupport = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Housing,
                secondaryCategory: CityBudgetCategory.General);
            decimal commerceSupport = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Commerce,
                secondaryCategory: CityBudgetCategory.General);
            decimal infrastructureSupport = ResolveAllocationSupport(
                allocations: allocations,
                primaryCategory: CityBudgetCategory.Infrastructure,
                secondaryCategory: CityBudgetCategory.Operations);
            decimal overallPressure = ResolveBudgetPressure(
                budget: budget,
                allocations: allocations);

            decimal landlordLiquidity = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.Landlord,
                CityBusinessKind.MunicipalVendor);
            decimal utilityLiquidity = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.Utility,
                CityBusinessKind.MunicipalVendor);
            decimal retailLiquidity = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.RetailStore,
                CityBusinessKind.Service);
            decimal employerLiquidity = ResolveBusinessSupport(
                businesses: businesses,
                CityBusinessKind.Employer,
                CityBusinessKind.Service,
                CityBusinessKind.Manufacturer,
                CityBusinessKind.Utility,
                CityBusinessKind.MunicipalVendor);

            decimal wageTarget = state.BaseWageMultiplier *
                                 Clamp(
                                     value: 0.90m +
                                            (employerLiquidity * 0.24m) +
                                            (commerceSupport * 0.08m) -
                                            (overallPressure * 0.10m),
                                     min: 0.75m,
                                     max: 1.35m);
            decimal retailTarget = state.BaseRetailPriceMultiplier *
                                   Clamp(
                                       value: 1.09m -
                                              (retailLiquidity * 0.14m) -
                                              (commerceSupport * 0.05m) +
                                              (overallPressure * 0.08m),
                                       min: 0.75m,
                                       max: 1.45m);
            decimal housingTarget = state.BaseHousingCostMultiplier *
                                    Clamp(
                                        value: 1.07m -
                                               (housingSupport * 0.10m) -
                                               (landlordLiquidity * 0.08m) +
                                               (overallPressure * 0.12m),
                                        min: 0.75m,
                                        max: 1.50m);
            decimal utilityTarget = state.BaseUtilityCostMultiplier *
                                    Clamp(
                                        value: 1.04m -
                                               (infrastructureSupport * 0.08m) -
                                               (utilityLiquidity * 0.09m) +
                                               (overallPressure * 0.10m),
                                        min: 0.75m,
                                        max: 1.45m);

            decimal wageMultiplier = Smooth(
                current: state.WageMultiplier,
                target: wageTarget,
                factor: 0.35m);
            decimal retailMultiplier = Smooth(
                current: state.RetailPriceMultiplier,
                target: retailTarget,
                factor: 0.35m);
            decimal housingMultiplier = Smooth(
                current: state.HousingCostMultiplier,
                target: housingTarget,
                factor: 0.30m);
            decimal utilityMultiplier = Smooth(
                current: state.UtilityCostMultiplier,
                target: utilityTarget,
                factor: 0.30m);

            decimal costOfLivingIndex = decimal.Round(
                d: (retailMultiplier + housingMultiplier + utilityMultiplier) / 3m,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            decimal affordabilityIndex = decimal.Round(
                d: Clamp(
                    value: wageMultiplier /
                    Math.Max(
                        val1: 0.35m,
                        val2: costOfLivingIndex),
                    min: 0.45m,
                    max: 1.60m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            return new CityEconomyCostProfileSnapshot(
                WageMultiplier: wageMultiplier,
                RetailPriceMultiplier: retailMultiplier,
                HousingCostMultiplier: housingMultiplier,
                UtilityCostMultiplier: utilityMultiplier,
                CostOfLivingIndex: costOfLivingIndex,
                AffordabilityIndex: affordabilityIndex,
                EvaluatedAtUtc: asOfUtc);
        }

        private static string NormalizeEconomyProfile(string? economyProfile)
        {
            return economyProfile?.Trim()
                      .ToUpperInvariant() ??
                   "BALANCED";
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
                    min: 0m,
                    max: 1.35m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveSingleAllocationSupport(CityBudgetAllocation? allocation)
        {
            if (allocation is null || allocation.TargetAmount.Amount <= 0m)
                return 0.65m;

            decimal spentRatio = allocation.TotalSpent.Amount / allocation.TargetAmount.Amount;
            decimal availableRatio = allocation.GetAvailableAmount()
                                        .Amount /
                                     allocation.TargetAmount.Amount;

            return Clamp(
                value: 0.72m +
                       (availableRatio * 0.35m) -
                       (Math.Max(
                            val1: 0m,
                            val2: spentRatio - 1m) *
                        0.28m),
                min: 0.20m,
                max: 1.30m);
        }

        private static decimal ResolveBudgetPressure(
            CityBudget? budget,
            IReadOnlyCollection<CityBudgetAllocation> allocations)
        {
            if (budget is null)
                return 0.50m;

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
                    value: reservePressure + (overrunPressure * 0.60m),
                    min: 0m,
                    max: 1m),
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
                return 0.70m;

            decimal support = scopedBusinesses.Average(x =>
            {
                decimal capitalBase = Math.Max(
                    val1: 1m,
                    val2: x.TotalCapitalInjections.Amount);
                decimal taxBurden = x.TaxReserve.Amount / capitalBase;
                decimal balanceRatio = x.Balance.Amount / capitalBase;

                return Clamp(
                    value: 0.75m +
                           (balanceRatio * 0.60m) -
                           (taxBurden * 0.20m),
                    min: 0m,
                    max: 1.40m);
            });

            return decimal.Round(
                d: support,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal Smooth(
            decimal current,
            decimal target,
            decimal factor)
        {
            return decimal.Round(
                d: current + ((target - current) * factor),
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
