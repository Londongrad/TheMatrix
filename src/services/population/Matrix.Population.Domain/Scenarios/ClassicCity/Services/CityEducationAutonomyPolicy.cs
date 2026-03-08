using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEducationAutonomyPolicy
    {
        public bool Apply(
            Person person,
            DateOnly previousDate,
            DateOnly currentDate,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools)
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
            {
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

            if (currentAgeYears >= 18 &&
                person.Employment.Status == EmploymentStatus.Student &&
                person.EducationLevel <= EducationLevel.UpperSecondary)
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
                _ => EducationLevel.Postgraduate,
            };
        }

        private static EducationInstitutionId ResolveInstitutionId(
            Person person,
            EducationLevel educationLevel,
            IDictionary<EducationLevel, List<EducationInstitutionId>> institutionPools)
        {
            if (!institutionPools.TryGetValue(educationLevel, out List<EducationInstitutionId>? levelPool))
            {
                levelPool = [];
                institutionPools[educationLevel] = levelPool;
            }

            if (levelPool.Count == 0)
            {
                EducationInstitutionId created = EducationInstitutionId.New();
                levelPool.Add(created);
                return created;
            }

            int stableIndex = Math.Abs(person.Id.Value.GetHashCode()) % levelPool.Count;
            return levelPool[stableIndex];
        }
    }
}
