using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    public static class EmploymentRules
    {
        public static void ValidateCombination(
            EmploymentStatus status,
            Job? job,
            LifeStatus lifeStatus,
            AgeGroup ageGroup)
        {
            GuardHelper.AgainstInvalidEnum(status, nameof(status));
            GuardHelper.AgainstInvalidEnum(lifeStatus, nameof(lifeStatus));
            GuardHelper.AgainstInvalidEnum(ageGroup, nameof(ageGroup));

            // 1. Мёртвый человек
            if (lifeStatus == LifeStatus.Deceased)
            {
                if (status != EmploymentStatus.None)
                    throw PopulationErrors.DeceasedPersonEmploymentStatusMustBeNone(nameof(status));
                if (job is not null)
                    throw PopulationErrors.DeceasedPersonCannotHaveJob(nameof(job));
                return;
            }

            // 2. Детям нельзя работать
            if (ageGroup is AgeGroup.Child or AgeGroup.Youth &&
                status == EmploymentStatus.Employed)
            {
                throw PopulationErrors.ChildCannotBeEmployed(nameof(status));
            }

            // 3. Пенсионер не может работать или быть студентом
            if (ageGroup is AgeGroup.Senior &&
                (status is EmploymentStatus.Employed or EmploymentStatus.Student))
            {
                throw PopulationErrors.RetiredPersonCannotBeEmployedOrStudent(nameof(status));
            }

            // 4. Работает → Job обязателен
            if (status == EmploymentStatus.Employed && job is null)
            {
                throw PopulationErrors.EmployedPersonMustHaveJob(nameof(job));
            }

            // 5. Не работает → Job должен быть null
            if (status != EmploymentStatus.Employed && job is not null)
            {
                throw PopulationErrors.OnlyEmployedPersonCanHaveJob(nameof(job));
            }
        }
    }
}
