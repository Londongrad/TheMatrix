using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Person
    {
        public PersonId Id { get; }
        public HouseholdId HouseholdId { get; }
        public DistrictId DistrictId { get; }

        public Age Age { get; private set; }
        public AgeGroup AgeGroup => AgeGroupRules.GetAgeGroup(Age);

        public HappinessLevel Happiness { get; private set; }

        public EmploymentStatus EmploymentStatus { get; private set; }
        public Job? Job { get; private set; }

        public Person(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            Age age,
            EmploymentStatus employmentStatus,
            HappinessLevel initialHappiness,
            Job? job = null)
        {
            Id = id;
            HouseholdId = householdId;
            DistrictId = districtId;

            Age = GuardHelper.AgainstNull(age, nameof(Age));
            EmploymentStatus = GuardHelper.AgainstInvalidEnum(employmentStatus, nameof(EmploymentStatus));
            Happiness = initialHappiness;
            Job = job;

            EnsureConsistency();
        }

        private void EnsureConsistency()
        {
            var group = AgeGroup;

            if (group == AgeGroup.Child && EmploymentStatus == EmploymentStatus.Employed)
                throw new DomainValidationException("Child cannot be employed", nameof(EmploymentStatus));

            if (group == AgeGroup.Retired &&
                (EmploymentStatus == EmploymentStatus.Employed || EmploymentStatus == EmploymentStatus.Student))
                throw new DomainValidationException("Retired person cannot be employed or student", nameof(EmploymentStatus));

            if (EmploymentStatus == EmploymentStatus.Employed && Job is null)
                throw new DomainValidationException("Employed person must have a job", nameof(Job));

            if (EmploymentStatus != EmploymentStatus.Employed && Job is not null)
                throw new DomainValidationException("Only employed person can have a job", nameof(Job));
        }

        public void ChangeHappiness(int delta)
        {
            Happiness = Happiness.WithDelta(delta);
        }

        public void AssignJob(Job job)
        {
            if (AgeGroup != AgeGroup.Adult)
                throw new DomainValidationException("Only adults can be employed", nameof(EmploymentStatus));

            Job = GuardHelper.AgainstNull(job, nameof(Job));
            EmploymentStatus = EmploymentStatus.Employed;

            ChangeHappiness(+10);

            EnsureConsistency();
        }

        public void Fire()
        {
            if (EmploymentStatus != EmploymentStatus.Employed)
                return;

            Job = null;
            EmploymentStatus = EmploymentStatus.Unemployed;

            ChangeHappiness(-10);

            EnsureConsistency();
        }

        public void IncreaseAge(int years)
        {
            Age = Age.Increase(years);

            if (AgeGroup == AgeGroup.Retired && EmploymentStatus != EmploymentStatus.Retired)
            {
                EmploymentStatus = EmploymentStatus.Retired;
                Job = null;
            }

            ChangeHappiness(-1);

            EnsureConsistency();
        }
    }
}
