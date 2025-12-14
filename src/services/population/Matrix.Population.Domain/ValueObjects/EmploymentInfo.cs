using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class EmploymentInfo
    {
        private EmploymentInfo() { }

        private EmploymentInfo(
            EmploymentStatus status,
            Job? job)
        {
            Status = status;
            Job = job;
        }

        public EmploymentStatus Status { get; }
        public Job? Job { get; }

        public static EmploymentInfo Create(
            EmploymentStatus status,
            Job? job,
            LifeStatus lifeStatus,
            AgeGroup ageGroup)
        {
            EmploymentRules.ValidateCombination(
                status: status,
                job: job,
                lifeStatus: lifeStatus,
                ageGroup: ageGroup);
            return new EmploymentInfo(
                status: status,
                job: job);
        }

        public EmploymentInfo Change(
            EmploymentStatus newStatus,
            Job? newJob,
            LifeStatus lifeStatus,
            AgeGroup ageGroup)
        {
            EmploymentRules.ValidateCombination(
                status: newStatus,
                job: newJob,
                lifeStatus: lifeStatus,
                ageGroup: ageGroup);
            return new EmploymentInfo(
                status: newStatus,
                job: newJob);
        }
    }
}
