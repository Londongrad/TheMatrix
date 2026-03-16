using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdCashflowPolicy
    {
        public CityResidentIncomeSettlementProfile BuildResidentIncome(
            Person resident,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);

            AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
            Money grossIncome = ResolveResidentGrossIncome(
                resident: resident,
                ageGroup: ageGroup);
            Money taxWithheld = grossIncome.Multiply(ResolveTaxRate(resident));

            return new CityResidentIncomeSettlementProfile(
                GrossIncome: grossIncome,
                TaxWithheld: taxWithheld,
                NetIncome: grossIncome.Subtract(taxWithheld));
        }

        public CityHouseholdCashflowProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(householdResidents);

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
                return new CityHouseholdCashflowProfile(
                    ResidentCount: 0,
                    GrossIncome: Money.Zero,
                    TaxWithheld: Money.Zero,
                    TakeHomeIncome: Money.Zero,
                    RetailTurnover: Money.Zero,
                    HousingExpense: Money.Zero,
                    DailyExpenses: Money.Zero,
                    DailyNet: Money.Zero);

            Money grossIncome = Money.Zero;
            Money taxWithheld = Money.Zero;
            Money retailTurnover = Money.Zero;
            int childCount = 0;
            int infantCount = 0;

            foreach (Person resident in activeResidents)
            {
                AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
                if (ageGroup is AgeGroup.Child or AgeGroup.Youth)
                    childCount++;

                if (resident.GetAge(currentDate)
                       .Years ==
                    0)
                    infantCount++;

                CityResidentIncomeSettlementProfile residentIncome = BuildResidentIncome(
                    resident: resident,
                    currentDate: currentDate);

                grossIncome = grossIncome.Add(residentIncome.GrossIncome);
                taxWithheld = taxWithheld.Add(residentIncome.TaxWithheld);
                retailTurnover = retailTurnover.Add(
                    ResolveResidentDailyExpense(
                        resident: resident,
                        ageGroup: ageGroup,
                        currentDate: currentDate));
            }

            Money housingExpense = ResolveHousingExpense(
                residentCount: activeResidents.Length,
                childCount: childCount,
                infantCount: infantCount,
                housingStatus: housingStatus);
            Money dailyExpenses = retailTurnover.Add(housingExpense);

            Money takeHomeIncome = grossIncome.Subtract(taxWithheld);
            Money dailyNet = takeHomeIncome.Subtract(dailyExpenses);

            return new CityHouseholdCashflowProfile(
                ResidentCount: activeResidents.Length,
                GrossIncome: grossIncome,
                TaxWithheld: taxWithheld,
                TakeHomeIncome: takeHomeIncome,
                RetailTurnover: retailTurnover,
                HousingExpense: housingExpense,
                DailyExpenses: dailyExpenses,
                DailyNet: dailyNet);
        }

        private static Money ResolveResidentGrossIncome(
            Person resident,
            AgeGroup ageGroup)
        {
            decimal amount = resident.Employment.Status switch
            {
                EmploymentStatus.Employed => ResolveEmploymentIncome(
                    resident: resident,
                    ageGroup: ageGroup),
                EmploymentStatus.Retired => 26m,
                EmploymentStatus.Student when ageGroup is AgeGroup.Adult or AgeGroup.Senior => 10m,
                EmploymentStatus.Student => 4m,
                _ => 0m
            };

            return Money.FromDecimal(amount);
        }

        private static decimal ResolveEmploymentIncome(
            Person resident,
            AgeGroup ageGroup)
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

            return decimal.Round(
                d: Math.Max(
                    val1: 12m,
                    val2: ageBase + educationBonus + traitBonus + wellbeingBonus + jobVariance),
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

        private static Money ResolveResidentDailyExpense(
            Person resident,
            AgeGroup ageGroup,
            DateOnly currentDate)
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

            if (resident.HasActiveIllness)
                amount += resident.CurrentIllnessSeverity switch
                {
                    IllnessSeverity.Mild => 3m,
                    IllnessSeverity.Moderate => 7m,
                    IllnessSeverity.Severe => 14m,
                    _ => 4m
                };

            return Money.FromDecimal(amount);
        }

        private static Money ResolveHousingExpense(
            int residentCount,
            int childCount,
            int infantCount,
            HousingStatus? housingStatus)
        {
            decimal amount = housingStatus == HousingStatus.Housed
                ? 10m +
                  (residentCount * 3m) +
                  (Math.Max(
                       val1: 0,
                       val2: residentCount - 3) *
                   2m) +
                  childCount +
                  (infantCount * 2m)
                : 6m + (residentCount * 1.5m);

            return Money.FromDecimal(
                decimal.Round(
                    d: amount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero));
        }
    }
}
