using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdPressurePolicy
    {
        public bool Apply(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(householdResidents);

            if (!resident.IsAlive || currentDate <= previousDate)
                return false;

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
                return false;

            int reviewWindows = ResolveDailyReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);

            if (reviewWindows <= 0)
                return false;

            HouseholdPressureEffect effect = ResolveEffect(
                resident: resident,
                householdResidents: activeResidents,
                housingStatus: housingStatus,
                financialStressState: financialStressState,
                currentDate: currentDate)
               .Scale(Math.Clamp(reviewWindows, 1, 3));

            if (!effect.HasAnyEffect)
                return false;

            int previousHappiness = resident.Happiness.Value;
            int previousEnergy = resident.Energy.Value;
            int previousStress = resident.Stress.Value;
            int previousSocialNeed = resident.SocialNeed.Value;

            if (effect.HappinessDelta != 0)
                resident.ChangeHappiness(effect.HappinessDelta);
            if (effect.EnergyDelta != 0)
                resident.ChangeEnergy(effect.EnergyDelta);
            if (effect.StressDelta != 0)
                resident.ChangeStress(effect.StressDelta);
            if (effect.SocialNeedDelta != 0)
                resident.ChangeSocialNeed(effect.SocialNeedDelta);

            return previousHappiness != resident.Happiness.Value ||
                   previousEnergy != resident.Energy.Value ||
                   previousStress != resident.Stress.Value ||
                   previousSocialNeed != resident.SocialNeed.Value;
        }

        private static HouseholdPressureEffect ResolveEffect(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate)
        {
            int householdSize = householdResidents.Count;
            int dependentCount = householdResidents.Count(x => x.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Youth);
            int infantCount = householdResidents.Count(x => x.GetAge(currentDate).Years == 0);
            int activeIllnessCount = householdResidents.Count(x => x.HasActiveIllness);
            int employedAdults = householdResidents.Count(x =>
                x.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior &&
                x.Employment.Status == EmploymentStatus.Employed);
            int studentResidents = householdResidents.Count(x => x.Employment.Status == EmploymentStatus.Student);

            bool hasDependents = dependentCount > 0;
            bool isParentOfDependent = householdResidents.Any(x =>
                x.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Youth &&
                (x.MotherId == resident.Id || x.FatherId == resident.Id));
            bool isParentOfInfant = householdResidents.Any(x =>
                x.GetAge(currentDate).Years == 0 &&
                (x.MotherId == resident.Id || x.FatherId == resident.Id));
            bool isolatedSingle = householdSize == 1 && resident.MaritalStatus is MaritalStatus.Single or MaritalStatus.Divorced or MaritalStatus.Widowed;

            int crowding = Math.Max(0, householdSize - 3);
            int supportShortfall = Math.Max(0, dependentCount - employedAdults - studentResidents);

            int happinessDelta = 0;
            int energyDelta = 0;
            int stressDelta = 0;
            int socialNeedDelta = 0;

            if (housingStatus == HousingStatus.Homeless)
            {
                happinessDelta -= 2;
                energyDelta -= 1;
                stressDelta += 2;
            }

            if (crowding > 0)
            {
                happinessDelta -= Math.Min(2, crowding);
                stressDelta += Math.Min(3, crowding);
            }

            if (supportShortfall > 0)
            {
                happinessDelta -= 1;
                stressDelta += Math.Min(2, supportShortfall);
            }

            if (activeIllnessCount > 0)
                stressDelta += 1;

            if (isolatedSingle)
            {
                socialNeedDelta += 2;
                happinessDelta -= 1;
            }
            else if (householdSize >= 2)
            {
                socialNeedDelta -= 1;
            }

            if (hasDependents && employedAdults > dependentCount)
                happinessDelta += 1;

            if (isParentOfDependent)
            {
                energyDelta -= 1;
                stressDelta += 1;
            }

            if (isParentOfInfant)
            {
                energyDelta -= 2;
                stressDelta += 2;
                happinessDelta += 1;
            }

            if (resident.HasActiveIllness)
            {
                energyDelta -= 1;
                happinessDelta -= 1;
            }

            if (IsRecentFinancialStress(financialStressState, currentDate))
            {
                decimal financialStressScore = financialStressState!.DistressScore;

                if (financialStressScore >= 0.35m)
                    happinessDelta -= 1;

                if (financialStressScore >= 0.60m)
                    stressDelta += 2;
                else if (financialStressScore > 0m)
                    stressDelta += 1;

                if (financialStressState.OverdueRentCount > 0)
                {
                    happinessDelta -= 1;
                    stressDelta += 1;
                }

                if (financialStressState.OverdueUtilityCount > 0)
                {
                    energyDelta -= 1;
                    stressDelta += 1;
                }
            }

            return new HouseholdPressureEffect(
                HappinessDelta: happinessDelta,
                EnergyDelta: energyDelta,
                StressDelta: stressDelta,
                SocialNeedDelta: socialNeedDelta);
        }

        private static int ResolveDailyReviewWindows(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            return Math.Clamp(currentDate.DayNumber - previousDate.DayNumber, 0, 3);
        }

        private static bool IsRecentFinancialStress(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            if (state is null)
                return false;

            DateOnly lastEvaluatedDate = DateOnly.FromDateTime(state.LastEvaluatedAtUtc.UtcDateTime);
            return currentDate.DayNumber - lastEvaluatedDate.DayNumber <= 45;
        }

        private sealed record HouseholdPressureEffect(
            int HappinessDelta,
            int EnergyDelta,
            int StressDelta,
            int SocialNeedDelta)
        {
            public bool HasAnyEffect =>
                HappinessDelta != 0 ||
                EnergyDelta != 0 ||
                StressDelta != 0 ||
                SocialNeedDelta != 0;

            public HouseholdPressureEffect Scale(int factor)
            {
                return new HouseholdPressureEffect(
                    HappinessDelta * factor,
                    EnergyDelta * factor,
                    StressDelta * factor,
                    SocialNeedDelta * factor);
            }
        }
    }
}
