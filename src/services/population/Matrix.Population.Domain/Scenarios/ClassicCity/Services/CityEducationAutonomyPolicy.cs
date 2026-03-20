using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEducationAutonomyPolicy
    {
        public bool Apply(
            Person person,
            DateOnly previousDate,
            DateOnly currentDate,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools,
            CityPopulationServiceQualityState? serviceQualityState = null)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(institutionPools);

            if (!person.IsAlive)
                return false;

            int previousAgeYears = person.GetAge(previousDate).Years;
            int currentAgeYears = person.GetAge(currentDate).Years;
            bool changed = false;

            EducationLevel? targetFloor = ResolveMandatoryEducationFloor(
                previousAgeYears: previousAgeYears,
                currentAgeYears: currentAgeYears);

            if (targetFloor.HasValue)
                while (person.EducationLevel < targetFloor.Value)
                {
                    EducationLevel nextLevel = ResolveNextEducationLevel(person.EducationLevel);
                    EducationInstitutionId institutionId = ResolveInstitutionId(
                        person: person,
                        educationLevel: nextLevel,
                        institutionPools: institutionPools);

                    person.GraduateTo(
                        newLevel: nextLevel,
                        institutionId: institutionId);
                    changed = true;
                }

            if (currentAgeYears is >= 3 and < 18)
            {
                if (person.EducationLevel == EducationLevel.None)
                {
                    EducationInstitutionId preschoolInstitutionId = ResolveInstitutionId(
                        person: person,
                        educationLevel: EducationLevel.Preschool,
                        institutionPools: institutionPools);

                    person.GraduateTo(
                        newLevel: EducationLevel.Preschool,
                        institutionId: preschoolInstitutionId);
                    changed = true;
                }

                if (person.Employment.Status is EmploymentStatus.None or EmploymentStatus.Unemployed)
                {
                    EducationInstitutionId institutionId = ResolveInstitutionId(
                        person: person,
                        educationLevel: person.EducationLevel,
                        institutionPools: institutionPools);

                    person.StartStudying(
                        currentDate: currentDate,
                        institutionId: institutionId);
                    changed = true;
                }

                return changed;
            }

            int postSecondaryReviewWindows = ResolvePostSecondaryReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);
            bool advancedPostSecondary = false;

            if (postSecondaryReviewWindows > 0)
            {
                advancedPostSecondary = TryAdvancePostSecondaryEducation(
                    person: person,
                    currentDate: currentDate,
                    reviewWindows: postSecondaryReviewWindows,
                    institutionPools: institutionPools,
                    serviceQualityState: serviceQualityState);
                changed = changed || advancedPostSecondary;

                if (!advancedPostSecondary)
                {
                    bool startedPostSecondary = TryStartPostSecondaryEducation(
                        person: person,
                        currentDate: currentDate,
                        reviewWindows: postSecondaryReviewWindows,
                        institutionPools: institutionPools,
                        serviceQualityState: serviceQualityState);
                    changed = changed || startedPostSecondary;
                }
            }

            if (person.Employment.Status == EmploymentStatus.Student &&
                postSecondaryReviewWindows > 0 &&
                ShouldStopStudying(
                    person: person,
                    currentAgeYears: currentAgeYears,
                    advancedPostSecondaryThisPass: advancedPostSecondary))
            {
                person.StopStudying(currentDate);
                changed = true;
            }

            return changed;
        }

        private static EducationLevel? ResolveMandatoryEducationFloor(
            int previousAgeYears,
            int currentAgeYears)
        {
            if (currentAgeYears < 3)
                return null;

            if (currentAgeYears < 7)
                return EducationLevel.Preschool;

            if (currentAgeYears < 13)
                return EducationLevel.Primary;

            if (currentAgeYears < 16)
                return EducationLevel.LowerSecondary;

            if (currentAgeYears < 18)
                return EducationLevel.UpperSecondary;

            return previousAgeYears < 18
                ? EducationLevel.UpperSecondary
                : null;
        }

        private static EducationLevel ResolveNextEducationLevel(EducationLevel currentLevel)
        {
            return currentLevel switch
            {
                EducationLevel.None => EducationLevel.Preschool,
                EducationLevel.Preschool => EducationLevel.Primary,
                EducationLevel.Primary => EducationLevel.LowerSecondary,
                EducationLevel.LowerSecondary => EducationLevel.UpperSecondary,
                EducationLevel.UpperSecondary => EducationLevel.Vocational,
                EducationLevel.Vocational => EducationLevel.Higher,
                EducationLevel.Higher => EducationLevel.Postgraduate,
                _ => EducationLevel.Postgraduate
            };
        }

        private static bool TryAdvancePostSecondaryEducation(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools,
            CityPopulationServiceQualityState? serviceQualityState)
        {
            if (person.Employment.Status != EmploymentStatus.Student)
                return false;

            EducationLevel? targetLevel = ResolveContinuationTargetLevel(person.EducationLevel);
            if (!targetLevel.HasValue)
                return false;

            if (!RollOccurs(
                    personId: person.Id,
                    currentDate: currentDate,
                    salt: 1_921,
                    chancePerReview: ResolveAdvanceChancePerReview(
                        person: person,
                        educationQualityIndex: ResolveEducationQualityIndex(serviceQualityState),
                        continuationLevel: targetLevel.Value),
                    reviewWindows: reviewWindows))
                return false;

            EducationInstitutionId institutionId = ResolveInstitutionId(
                person: person,
                educationLevel: targetLevel.Value,
                institutionPools: institutionPools);

            person.GraduateTo(
                newLevel: targetLevel.Value,
                institutionId: institutionId);
            return true;
        }

        private static bool TryStartPostSecondaryEducation(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools,
            CityPopulationServiceQualityState? serviceQualityState)
        {
            if (person.Employment.Status is not (EmploymentStatus.None or EmploymentStatus.Unemployed))
                return false;

            int currentAgeYears = person.GetAge(currentDate).Years;
            if (currentAgeYears is < 18 or > 23 || person.EducationLevel != EducationLevel.UpperSecondary)
                return false;

            if (!RollOccurs(
                    personId: person.Id,
                    currentDate: currentDate,
                    salt: 1_973,
                    chancePerReview: ResolveEnrollmentChancePerReview(
                        person: person,
                        educationQualityIndex: ResolveEducationQualityIndex(serviceQualityState)),
                    reviewWindows: reviewWindows))
                return false;

            EducationInstitutionId institutionId = ResolveInstitutionId(
                person: person,
                educationLevel: EducationLevel.Vocational,
                institutionPools: institutionPools);

            person.GraduateTo(
                newLevel: EducationLevel.Vocational,
                institutionId: institutionId);
            person.StartStudying(
                currentDate: currentDate,
                institutionId: institutionId);
            return true;
        }

        private static EducationLevel? ResolveContinuationTargetLevel(EducationLevel currentLevel)
        {
            return currentLevel switch
            {
                EducationLevel.UpperSecondary => EducationLevel.Vocational,
                EducationLevel.Vocational => EducationLevel.Higher,
                EducationLevel.Higher => EducationLevel.Postgraduate,
                _ => null
            };
        }

        private static bool ShouldStopStudying(
            Person person,
            int currentAgeYears,
            bool advancedPostSecondaryThisPass)
        {
            if (advancedPostSecondaryThisPass)
                return false;

            return person.EducationLevel switch
            {
                <= EducationLevel.UpperSecondary => currentAgeYears >= 18,
                EducationLevel.Vocational => currentAgeYears >= 22,
                EducationLevel.Higher => currentAgeYears >= 25,
                EducationLevel.Postgraduate => currentAgeYears >= 29,
                _ => false
            };
        }

        private static int ResolvePostSecondaryReviewWindows(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            int previousWindow = previousDate.DayNumber / 30;
            int currentWindow = currentDate.DayNumber / 30;
            return Math.Clamp(
                value: currentWindow - previousWindow,
                min: 0,
                max: 6);
        }

        private static double ResolveAdvanceChancePerReview(
            Person person,
            decimal educationQualityIndex,
            EducationLevel continuationLevel)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double health = Normalize(person.Health.Value);
            double energy = Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);
            double qualityDelta = (double)(educationQualityIndex - 1m);

            double chance = 0.010d +
                            (discipline * 0.032d) +
                            (optimism * 0.014d) +
                            (health * 0.015d) +
                            (energy * 0.015d) -
                            (stress * 0.024d) +
                            (qualityDelta * 0.16d);

            if (continuationLevel == EducationLevel.Higher)
                chance += 0.008d;
            else if (continuationLevel == EducationLevel.Postgraduate)
                chance += 0.004d;

            return Math.Clamp(
                value: chance,
                min: 0.001d,
                max: 0.22d);
        }

        private static double ResolveEnrollmentChancePerReview(
            Person person,
            decimal educationQualityIndex)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double health = Normalize(person.Health.Value);
            double energy = Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);
            double qualityDelta = (double)(educationQualityIndex - 1m);

            return Math.Clamp(
                value: 0.004d +
                       (discipline * 0.020d) +
                       (optimism * 0.010d) +
                       (health * 0.010d) +
                       (energy * 0.010d) -
                       (stress * 0.020d) +
                       (qualityDelta * 0.12d),
                min: 0.001d,
                max: 0.14d);
        }

        private static double Normalize(int value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
        }

        private static decimal ResolveEducationQualityIndex(CityPopulationServiceQualityState? serviceQualityState)
        {
            return serviceQualityState?.EducationQualityIndex ?? 1m;
        }

        private static bool RollOccurs(
            PersonId personId,
            DateOnly currentDate,
            int salt,
            double chancePerReview,
            int reviewWindows)
        {
            if (reviewWindows <= 0 || chancePerReview <= 0d)
                return false;

            double combinedChance = 1d -
                                    Math.Pow(
                                        x: 1d - chancePerReview,
                                        y: reviewWindows);

            return GetStableFraction(
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt) <
                   combinedChance;
        }

        private static double GetStableFraction(
            PersonId personId,
            DateOnly currentDate,
            int salt)
        {
            int hash = HashCode.Combine(
                personId.Value,
                currentDate.DayNumber,
                salt);
            uint normalized = unchecked((uint)hash);
            return normalized / (double)uint.MaxValue;
        }

        private static EducationInstitutionId ResolveInstitutionId(
            Person person,
            EducationLevel educationLevel,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools)
        {
            if (!institutionPools.TryGetValue(
                    key: educationLevel,
                    value: out List<EducationInstitutionId>? levelPool))
            {
                levelPool = [];
                institutionPools[educationLevel] = levelPool;
            }

            if (levelPool.Count == 0)
            {
                var created = EducationInstitutionId.New();
                levelPool.Add(created);
                return created;
            }

            int stableIndex = Math.Abs(person.Id.Value.GetHashCode()) % levelPool.Count;
            return levelPool[stableIndex];
        }
    }
}
