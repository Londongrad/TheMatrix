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
            GuardHelper.AgainstInvalidEnum(value: status, propertyName: nameof(status));
            GuardHelper.AgainstInvalidEnum(value: lifeStatus, propertyName: nameof(lifeStatus));
            GuardHelper.AgainstInvalidEnum(value: ageGroup, propertyName: nameof(ageGroup));

            // 1. Мёртвый человек
            if (lifeStatus == LifeStatus.Deceased)
            {
                if (status != EmploymentStatus.None)
                    throw DomainErrorsFactory.DeceasedPersonEmploymentStatusMustBeNone(nameof(status));
                if (job is not null)
                    throw DomainErrorsFactory.DeceasedPersonCannotHaveJob(nameof(job));
                return;
            }

            // 2. Детям нельзя работать
            if (ageGroup is AgeGroup.Child or AgeGroup.Youth &&
                status == EmploymentStatus.Employed)
                throw DomainErrorsFactory.ChildCannotBeEmployed(nameof(status));

            // 3. Пенсионер не может работать или быть студентом
            if (ageGroup is AgeGroup.Senior &&
                status is EmploymentStatus.Employed or EmploymentStatus.Student)
                throw DomainErrorsFactory.RetiredPersonCannotBeEmployedOrStudent(nameof(status));

            // 4. Работает → Job обязателен
            if (status == EmploymentStatus.Employed && job is null)
                throw DomainErrorsFactory.EmployedPersonMustHaveJob(nameof(job));

            // 5. Не работает → Job должен быть null
            if (status != EmploymentStatus.Employed && job is not null)
                throw DomainErrorsFactory.OnlyEmployedPersonCanHaveJob(nameof(job));
        }
    }
}
