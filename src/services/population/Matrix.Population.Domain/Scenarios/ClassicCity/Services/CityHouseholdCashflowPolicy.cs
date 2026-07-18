using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdCashflowPolicy
    {
        public CityResidentIncomeSettlementProfile BuildResidentIncome(
            Person resident,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null,
            decimal incomeMultiplier = 1m)
        {
            return BuildResidentIncome(
                resident: resident,
                economicContext: CityResidentEconomicContext.Neutral,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState,
                incomeMultiplier: incomeMultiplier);
        }

        public CityResidentIncomeSettlementProfile BuildResidentIncome(
            Person resident,
            CityResidentEconomicContext economicContext,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null,
            decimal incomeMultiplier = 1m)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(economicContext);

            AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
            Money grossIncome = ResolveResidentGrossIncome(
                    resident: resident,
                    ageGroup: ageGroup,
                    economicContext: economicContext,
                    costOfLivingState: costOfLivingState)
               .Multiply(
                    Math.Clamp(
                        value: incomeMultiplier,
                        min: 0m,
                        max: 1m));
            Money taxWithheld = grossIncome.Multiply(ResolveTaxRate(resident));

            return new CityResidentIncomeSettlementProfile(
                GrossIncome: grossIncome,
                TaxWithheld: taxWithheld,
                NetIncome: grossIncome.Subtract(taxWithheld));
        }

        public CityHouseholdCashflowProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null)
        {
            return Build(
                householdResidents: householdResidents,
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                housingStatus: housingStatus,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState);
        }

        public CityHouseholdCashflowProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, CityResidentEconomicContext> economicContextsByResidentId,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null)
        {
            ArgumentNullException.ThrowIfNull(householdResidents);
            ArgumentNullException.ThrowIfNull(economicContextsByResidentId);

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
                return CreateEmptyProfile(costOfLivingState);

            Money grossIncome = Money.Zero;
            Money taxWithheld = Money.Zero;
            Money retailTurnover = Money.Zero;
            Money retailStoreSpend = Money.Zero;
            Money serviceSpend = Money.Zero;
            Money municipalSpend = Money.Zero;
            int childCount = 0;
            int infantCount = 0;

            foreach (Person resident in activeResidents)
            {
                CityResidentEconomicContext economicContext = economicContextsByResidentId.GetValueOrDefault(
                    resident.Id,
                    CityResidentEconomicContext.Neutral);
                AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
                if (ageGroup is AgeGroup.Child or AgeGroup.Youth)
                    childCount++;

                if (resident.GetAge(currentDate)
                       .Years ==
                    0)
                    infantCount++;

                CityResidentIncomeSettlementProfile residentIncome = BuildResidentIncome(
                    resident: resident,
                    economicContext: economicContext,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);

                grossIncome = grossIncome.Add(residentIncome.GrossIncome);
                taxWithheld = taxWithheld.Add(residentIncome.TaxWithheld);
                (
                    Money residentRetailTurnover,
                    Money residentRetailStoreSpend,
                    Money residentServiceSpend,
                    Money residentMunicipalSpend) = ResolveResidentDailyExpenseBreakdown(
                    resident: resident,
                    ageGroup: ageGroup,
                    economicContext: economicContext,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);

                retailTurnover = retailTurnover.Add(residentRetailTurnover);
                retailStoreSpend = retailStoreSpend.Add(residentRetailStoreSpend);
                serviceSpend = serviceSpend.Add(residentServiceSpend);
                municipalSpend = municipalSpend.Add(residentMunicipalSpend);
            }

            Money housingExpense = ResolveHousingExpense(
                residentCount: activeResidents.Length,
                childCount: childCount,
                infantCount: infantCount,
                housingStatus: housingStatus,
                costOfLivingState: costOfLivingState);
            Money dailyExpenses = retailTurnover.Add(housingExpense);

            Money takeHomeIncome = grossIncome.Subtract(taxWithheld);
            Money dailyNet = takeHomeIncome.Subtract(dailyExpenses);

            return new CityHouseholdCashflowProfile(
                ResidentCount: activeResidents.Length,
                GrossIncome: grossIncome,
                TaxWithheld: taxWithheld,
                TakeHomeIncome: takeHomeIncome,
                RetailTurnover: retailTurnover,
                RetailStoreSpend: retailStoreSpend,
                ServiceSpend: serviceSpend,
                MunicipalSpend: municipalSpend,
                HousingExpense: housingExpense,
                DailyExpenses: dailyExpenses,
                DailyNet: dailyNet,
                WageMultiplier: ResolveWageMultiplier(costOfLivingState),
                RetailPriceMultiplier: ResolveRetailPriceMultiplier(costOfLivingState),
                HousingCostMultiplier: ResolveHousingCostMultiplier(costOfLivingState),
                UtilityCostMultiplier: ResolveUtilityCostMultiplier(costOfLivingState),
                CostOfLivingIndex: ResolveCostOfLivingIndex(costOfLivingState),
                AffordabilityIndex: ResolveAffordabilityIndex(costOfLivingState));
        }

        private static CityHouseholdCashflowProfile CreateEmptyProfile(
            CityPopulationCostOfLivingState? costOfLivingState)
        {
            return new CityHouseholdCashflowProfile(
                ResidentCount: 0,
                GrossIncome: Money.Zero,
                TaxWithheld: Money.Zero,
                TakeHomeIncome: Money.Zero,
                RetailTurnover: Money.Zero,
                RetailStoreSpend: Money.Zero,
                ServiceSpend: Money.Zero,
                MunicipalSpend: Money.Zero,
                HousingExpense: Money.Zero,
                DailyExpenses: Money.Zero,
                DailyNet: Money.Zero,
                WageMultiplier: ResolveWageMultiplier(costOfLivingState),
                RetailPriceMultiplier: ResolveRetailPriceMultiplier(costOfLivingState),
                HousingCostMultiplier: ResolveHousingCostMultiplier(costOfLivingState),
                UtilityCostMultiplier: ResolveUtilityCostMultiplier(costOfLivingState),
                CostOfLivingIndex: ResolveCostOfLivingIndex(costOfLivingState),
                AffordabilityIndex: ResolveAffordabilityIndex(costOfLivingState));
        }

        private static Money ResolveResidentGrossIncome(
            Person resident,
            AgeGroup ageGroup,
            CityResidentEconomicContext economicContext,
            CityPopulationCostOfLivingState? costOfLivingState)
        {
            decimal amount = resident.Employment.Status switch
            {
                EmploymentStatus.Employed => ResolveEmploymentIncome(
                    resident: resident,
                    ageGroup: ageGroup,
                    costOfLivingState: costOfLivingState),
                EmploymentStatus.Retired => 26m,
                EmploymentStatus.Student when ageGroup is AgeGroup.Adult or AgeGroup.Senior => 10m,
                EmploymentStatus.Student => 4m,
                _ => economicContext.DailyTransferIncome.Amount
            };

            return Money.FromDecimal(amount);
        }

        private static decimal ResolveEmploymentIncome(
            Person resident,
            AgeGroup ageGroup,
            CityPopulationCostOfLivingState? costOfLivingState)
        {
            decimal ageBase = ageGroup switch
            {
                AgeGroup.Senior => 42m,
                _ => 48m
            };

            decimal educationBonus = resident.EducationLevel switch
            {
                EducationLevel.None => 0m,
                EducationLevel.Preschool => 0m,
                EducationLevel.Primary => 1m,
                EducationLevel.LowerSecondary => 3m,
                EducationLevel.UpperSecondary => 6m,
                EducationLevel.Vocational => 10m,
                EducationLevel.Higher => 14m,
                EducationLevel.Postgraduate => 18m,
                _ => 4m
            };

            decimal traitBonus =
                (decimal)(resident.Personality.Discipline / 12d) + (decimal)(resident.Personality.Optimism / 20d);
            decimal wellbeingBonus =
                (decimal)(resident.Health.Value / 18d) +
                (decimal)(resident.Energy.Value / 22d) -
                (decimal)(resident.Stress.Value / 28d);
            decimal jobVariance = ResolveJobVariance(resident.Employment.Job?.Title);
            decimal baseAmount = decimal.Round(
                d: Math.Max(
                    val1: 12m,
                    val2: ageBase + educationBonus + traitBonus + wellbeingBonus + jobVariance),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);

            return decimal.Round(
                d: baseAmount * ResolveWageMultiplier(costOfLivingState),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveJobVariance(string? jobTitle)
        {
            if (string.IsNullOrWhiteSpace(jobTitle))
                return 0m;

            unchecked
            {
                int hash = 17;
                foreach (char ch in jobTitle.Trim())
                    hash = (hash * 31) + ch;

                return Math.Abs(hash % 9);
            }
        }

        private static decimal ResolveTaxRate(Person resident)
        {
            return resident.Employment.Status == EmploymentStatus.Employed
                ? 0.13m
                : 0m;
        }

        private static (Money total, Money retailStore, Money service, Money municipal)
            ResolveResidentDailyExpenseBreakdown(
                Person resident,
                AgeGroup ageGroup,
                CityResidentEconomicContext economicContext,
                DateOnly currentDate,
                CityPopulationCostOfLivingState? costOfLivingState)
        {
            decimal amount = ageGroup switch
            {
                AgeGroup.Adult => 10m,
                AgeGroup.Senior => 9m,
                AgeGroup.Youth => 7m,
                AgeGroup.Child => 6m,
                _ => 6m
            };

            if (resident.GetAge(currentDate)
                   .Years ==
                0)
                amount += 2m;

            if (resident.FunctionalCapacity.Value < 100)
                amount += resident.FunctionalCapacity.Value switch
                {
                    >= 80 => 3m,
                    >= 50 => 7m,
                    _ => 14m
                };

            decimal totalAmount = decimal.Round(
                d: amount * ResolveRetailPriceMultiplier(costOfLivingState),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);

            if (totalAmount <= 0m)
                return (Money.Zero, Money.Zero, Money.Zero, Money.Zero);

            (
                decimal retailStoreShare,
                decimal serviceShare,
                decimal municipalShare) = ResolveSpendShares(
                resident: resident,
                ageGroup: ageGroup,
                economicContext: economicContext,
                currentDate: currentDate);

            decimal retailStoreAmount = decimal.Round(
                d: totalAmount * retailStoreShare,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            decimal serviceAmount = decimal.Round(
                d: totalAmount * serviceShare,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            decimal municipalAmount = decimal.Round(
                d: totalAmount - retailStoreAmount - serviceAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);

            if (municipalAmount < 0m)
            {
                serviceAmount = decimal.Max(
                    x: 0m,
                    y: serviceAmount + municipalAmount);
                municipalAmount = decimal.Round(
                    d: totalAmount - retailStoreAmount - serviceAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero);
            }

            return (
                Money.FromDecimal(totalAmount),
                Money.FromDecimal(retailStoreAmount),
                Money.FromDecimal(serviceAmount),
                Money.FromDecimal(municipalAmount));
        }

        private static (decimal retailStoreShare, decimal serviceShare, decimal municipalShare) ResolveSpendShares(
            Person resident,
            AgeGroup ageGroup,
            CityResidentEconomicContext economicContext,
            DateOnly currentDate)
        {
            decimal retailStoreShare = 0.72m;
            decimal serviceShare = 0.18m;
            decimal municipalShare = 0.10m;

            switch (ageGroup)
            {
                case AgeGroup.Child:
                case AgeGroup.Youth:
                    retailStoreShare += 0.08m;
                    serviceShare -= 0.05m;
                    municipalShare -= 0.03m;
                    break;

                case AgeGroup.Senior:
                    retailStoreShare -= 0.04m;
                    serviceShare += 0.06m;
                    municipalShare -= 0.02m;
                    break;
            }

            if (resident.GetAge(currentDate)
                   .Years ==
                0)
            {
                retailStoreShare += 0.06m;
                serviceShare -= 0.03m;
                municipalShare -= 0.03m;
            }

            switch (resident.Employment.Status)
            {
                case EmploymentStatus.Employed:
                    retailStoreShare -= 0.03m;
                    serviceShare += 0.04m;
                    municipalShare -= 0.01m;
                    break;

                case EmploymentStatus.Student:
                    retailStoreShare -= 0.03m;
                    serviceShare -= 0.01m;
                    municipalShare += 0.04m;
                    break;
            }

            retailStoreShare += economicContext.RetailStoreSpendShareAdjustment;
            serviceShare += economicContext.ServiceSpendShareAdjustment;
            municipalShare += economicContext.MunicipalSpendShareAdjustment;

            if (resident.FunctionalCapacity.Value < 100)
            {
                retailStoreShare -= 0.12m;
                serviceShare += resident.FunctionalCapacity.Value switch
                {
                    < 50 => 0.10m,
                    < 80 => 0.08m,
                    _ => 0.06m
                };
                municipalShare += resident.FunctionalCapacity.Value switch
                {
                    < 50 => 0.02m,
                    < 80 => 0.03m,
                    _ => 0.06m
                };
            }

            retailStoreShare = decimal.Max(
                x: 0.15m,
                y: retailStoreShare);
            serviceShare = decimal.Max(
                x: 0.05m,
                y: serviceShare);
            municipalShare = decimal.Max(
                x: 0.03m,
                y: municipalShare);

            decimal total = retailStoreShare + serviceShare + municipalShare;
            return (
                decimal.Round(
                    d: retailStoreShare / total,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                decimal.Round(
                    d: serviceShare / total,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero),
                decimal.Round(
                    d: municipalShare / total,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static Money ResolveHousingExpense(
            int residentCount,
            int childCount,
            int infantCount,
            HousingStatus? housingStatus,
            CityPopulationCostOfLivingState? costOfLivingState)
        {
            decimal baseAmount = housingStatus == HousingStatus.Housed
                ? 10m +
                  (residentCount * 3m) +
                  (Math.Max(
                       val1: 0,
                       val2: residentCount - 3) *
                   2m) +
                  childCount +
                  (infantCount * 2m)
                : 6m + (residentCount * 1.5m);

            decimal housingShare = housingStatus == HousingStatus.Housed
                ? 0.74m
                : 0.58m;
            decimal utilityShare = 1m - housingShare;
            decimal repricedAmount = (baseAmount * housingShare * ResolveHousingCostMultiplier(costOfLivingState)) +
                                     (baseAmount * utilityShare * ResolveUtilityCostMultiplier(costOfLivingState));

            return Money.FromDecimal(
                decimal.Round(
                    d: repricedAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static decimal ResolveWageMultiplier(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.WageMultiplier ?? 1m;
        }

        private static decimal ResolveRetailPriceMultiplier(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.RetailPriceMultiplier ?? 1m;
        }

        private static decimal ResolveHousingCostMultiplier(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.HousingCostMultiplier ?? 1m;
        }

        private static decimal ResolveUtilityCostMultiplier(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.UtilityCostMultiplier ?? 1m;
        }

        private static decimal ResolveCostOfLivingIndex(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.CostOfLivingIndex ?? 1m;
        }

        private static decimal ResolveAffordabilityIndex(CityPopulationCostOfLivingState? costOfLivingState)
        {
            return costOfLivingState?.AffordabilityIndex ?? 1m;
        }
    }
}
