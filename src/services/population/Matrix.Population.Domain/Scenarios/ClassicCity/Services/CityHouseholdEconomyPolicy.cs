using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdEconomyPolicy(
        CityHouseholdLivelihoodPolicy householdLivelihoodPolicy,
        CityHouseholdCashflowPolicy householdCashflowPolicy)
    {
        public CityHouseholdEconomyProfile Build(
            Household household,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null)
        {
            return Build(
                household: household,
                householdResidents: householdResidents,
                routineProfilesByResidentId: routineProfilesByResidentId,
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                housingStatus: housingStatus,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState);
        }

        public CityHouseholdEconomyProfile Build(
            Household household,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            IReadOnlyDictionary<PersonId, CityResidentEconomicContext> economicContextsByResidentId,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null)
        {
            ArgumentNullException.ThrowIfNull(household);
            ArgumentNullException.ThrowIfNull(householdResidents);
            ArgumentNullException.ThrowIfNull(routineProfilesByResidentId);
            ArgumentNullException.ThrowIfNull(economicContextsByResidentId);

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
                return new CityHouseholdEconomyProfile(
                    HousingStatus: housingStatus,
                    CashReserveAmount: household.CashReserve.Amount,
                    GrossDailyIncomeAmount: 0m,
                    DailyTaxAmount: 0m,
                    NetDailyIncomeAmount: 0m,
                    DailyExpenseAmount: 0m,
                    DailyNetAmount: 0m,
                    ReserveCoverageDays: 0d,
                    SupportUnits: 0d,
                    LivingCostUnits: 0d,
                    EconomicBalance: 0d,
                    StrainScore: 1d,
                    GrowthReadinessScore: 0d,
                    CostOfLivingIndex: costOfLivingState?.CostOfLivingIndex ?? 1m,
                    AffordabilityIndex: costOfLivingState?.AffordabilityIndex ?? 1m);

            CityHouseholdLivelihoodProfile livelihood = householdLivelihoodPolicy.Build(
                householdResidents: activeResidents,
                routineProfilesByResidentId: routineProfilesByResidentId,
                housingStatus: housingStatus,
                currentDate: currentDate);
            CityHouseholdCashflowProfile cashflow = householdCashflowPolicy.Build(
                householdResidents: activeResidents,
                economicContextsByResidentId: economicContextsByResidentId,
                housingStatus: housingStatus,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState);

            int retiredAdults = activeResidents.Count(x =>
                x.GetAgeGroup(currentDate) == AgeGroup.Senior &&
                x.Employment.Status == EmploymentStatus.Retired);

            double supportUnits = (double)(cashflow.TakeHomeIncome.Amount / 24m) +
                                  (livelihood.AdultProviderCount * 0.25d) +
                                  (livelihood.AdultStructuredParticipantCount * 0.15d) +
                                  (retiredAdults * 0.10d);
            double costOfLivingPressure = Math.Max(
                val1: 0d,
                val2: (double)(cashflow.CostOfLivingIndex - 1m));
            double affordabilityPressure = Math.Max(
                val1: 0d,
                val2: (double)(1m - cashflow.AffordabilityIndex));
            double livingCostUnits = (double)(cashflow.DailyExpenses.Amount / 26m) +
                                     (livelihood.DependentCount * 0.06d) +
                                     (livelihood.InfantCount * 0.08d) +
                                     (livelihood.FunctionalLimitationCount * 0.08d) +
                                     (costOfLivingPressure * 0.40d) +
                                     (affordabilityPressure * 0.55d);

            double reserveCoverageDays = cashflow.DailyExpenses.Amount <= 0m
                ? 12d
                : (double)(household.CashReserve.Amount / cashflow.DailyExpenses.Amount);
            double reserveBufferUnits = Math.Clamp(
                value: reserveCoverageDays / 6d,
                min: -1.8d,
                max: 2.4d);
            double netUnits = (double)(cashflow.DailyNet.Amount / 32m);
            double balance = reserveBufferUnits + netUnits;

            double strain = 0.46d -
                            (reserveBufferUnits * 0.16d) -
                            (netUnits * 0.22d) -
                            (livelihood.StabilityScore * 0.24d) +
                            (livelihood.DependentCount * 0.03d) +
                            (livelihood.FunctionalLimitationCount * 0.04d) +
                            (costOfLivingPressure * 0.14d) +
                            (affordabilityPressure * 0.18d);

            if (livelihood.AdultProviderCount == 0 && livelihood.AdultStructuredParticipantCount == 0)
                strain += 0.14d;

            if (household.CashReserve.IsNegative)
                strain += 0.18d;

            if (cashflow.DailyNet.Amount < 0m)
                strain += Math.Min(
                    val1: 0.18d,
                    val2: (double)(Math.Abs(cashflow.DailyNet.Amount) / 120m));

            double growthReadiness = Math.Clamp(
                value: 0.72d -
                       strain +
                       (livelihood.StabilityScore * 0.24d) +
                       (Math.Max(
                            val1: 0d,
                            val2: (double)(cashflow.AffordabilityIndex - 1m)) *
                        0.12d) -
                       (costOfLivingPressure * 0.08d) +
                       Math.Clamp(
                           value: reserveCoverageDays / 18d,
                           min: -0.18d,
                           max: 0.28d) +
                       (Math.Max(
                            val1: 0d,
                            val2: netUnits) *
                        0.08d),
                min: 0d,
                max: 1d);

            return new CityHouseholdEconomyProfile(
                HousingStatus: housingStatus,
                CashReserveAmount: household.CashReserve.Amount,
                GrossDailyIncomeAmount: cashflow.GrossIncome.Amount,
                DailyTaxAmount: cashflow.TaxWithheld.Amount,
                NetDailyIncomeAmount: cashflow.TakeHomeIncome.Amount,
                DailyExpenseAmount: cashflow.DailyExpenses.Amount,
                DailyNetAmount: cashflow.DailyNet.Amount,
                ReserveCoverageDays: reserveCoverageDays,
                SupportUnits: supportUnits,
                LivingCostUnits: livingCostUnits,
                EconomicBalance: balance,
                StrainScore: Math.Clamp(
                    value: strain,
                    min: 0d,
                    max: 1d),
                GrowthReadinessScore: growthReadiness,
                CostOfLivingIndex: cashflow.CostOfLivingIndex,
                AffordabilityIndex: cashflow.AffordabilityIndex);
        }

    }
}
