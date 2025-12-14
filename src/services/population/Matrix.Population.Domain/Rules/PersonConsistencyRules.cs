using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    /// <summary>
    ///     Содержит кросс-инварианты Person, которые завязаны на несколько VO:
    ///     жизнь/смерть, работа и возраст.
    /// </summary>
    public static class PersonConsistencyRules
    {
        /// <summary>
        ///     Проверяет согласованность статуса жизни и даты смерти.
        /// </summary>
        public static void ValidateLifeStatusAndSpan(LifeState life)
        {
            if (life.Status == LifeStatus.Alive && life.DeathDate is not null)
                throw DomainErrorsFactory.AlivePersonCannotHaveDeathDate(nameof(LifeState.Status));

            if (life.Status == LifeStatus.Deceased && life.DeathDate is null)
                throw DomainErrorsFactory.DeceasedPersonMustHaveDeathDate(nameof(LifeState.Status));
        }

        /// <summary>
        ///     Проверки для уже умершего человека:
        ///     дата смерти, работа, здоровье.
        /// </summary>
        public static void ValidateForDead(
            LifeState life,
            EmploymentInfo employment)
        {
            if (life.IsAlive)
                throw DomainErrorsFactory.EnsureConsistencyForDeadCalledForAlivePerson(nameof(LifeState.Status));

            if (life.DeathDate is null)
                throw DomainErrorsFactory.DeceasedPersonMustHaveDeathDate(nameof(LifeState.Span));

            if (employment.Status != EmploymentStatus.None)
                throw DomainErrorsFactory.DeceasedPersonEmploymentStatusMustBeNone(nameof(EmploymentInfo.Status));

            if (employment.Job is not null)
                throw DomainErrorsFactory.DeceasedPersonCannotHaveJob(nameof(EmploymentInfo.Job));

            if (life.Health.Value != 0)
                throw DomainErrorsFactory.DeceasedPersonMustHaveZeroHealth(nameof(LifeState.Health));
        }

        /// <summary>
        ///     Проверяет, что занятость согласована с возрастной группой
        ///     и наличием/отсутствием работы.
        /// </summary>
        public static void ValidateEmploymentForAge(
            AgeGroup ageGroup,
            EmploymentInfo employment)
        {
            if (ageGroup == AgeGroup.Child &&
                employment.Status == EmploymentStatus.Employed)
                throw DomainErrorsFactory.ChildCannotBeEmployed(nameof(EmploymentInfo.Status));

            if (ageGroup == AgeGroup.Senior &&
                (employment.Status == EmploymentStatus.Employed ||
                 employment.Status == EmploymentStatus.Student))
                throw DomainErrorsFactory.RetiredPersonCannotBeEmployedOrStudent(nameof(EmploymentInfo.Status));

            if (employment.Status == EmploymentStatus.Employed && employment.Job is null)
                throw DomainErrorsFactory.EmployedPersonMustHaveJob(nameof(EmploymentInfo.Job));

            if (employment.Status != EmploymentStatus.Employed && employment.Job is not null)
                throw DomainErrorsFactory.OnlyEmployedPersonCanHaveJob(nameof(EmploymentInfo.Job));
        }
    }
}
