using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class EmploymentInfo
    {
        public EmploymentStatus Status { get; }
        public Job? Job { get; }

        private EmploymentInfo(EmploymentStatus status, Job? job)
        {
            Status = status;
            Job = job;
        }

        /// <summary>
        /// Базовое создание. Используется в конструкторе Person.
        /// </summary>
        public static EmploymentInfo Create(
            EmploymentStatus status,
            Job? job,
            LifeStatus lifeStatus,
            AgeGroup ageGroup)
        {
            EmploymentRules.ValidateCombination(status, job, lifeStatus, ageGroup);
            return new EmploymentInfo(status, job);
        }

        public EmploymentInfo Change(
            EmploymentStatus newStatus,
            Job? newJob,
            LifeStatus lifeStatus,
            AgeGroup ageGroup)
        {
            EmploymentRules.ValidateCombination(newStatus, newJob, lifeStatus, ageGroup);
            return new EmploymentInfo(newStatus, newJob);
        }
    }
}
