using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Rules
{
    public sealed class PersonConsistencyRulesTests
    {
        [Fact]
        public void ValidateLifeStatusAndSpan_WhenStateIsValid_DoesNotThrow()
        {
            var validLife = LifeState.Create(
                status: LifeStatus.Alive,
                span: LifeSpan.FromDates(
                    birthDate: new DateOnly(
                        year: 2030,
                        month: 1,
                        day: 1),
                    deathDate: null),
                health: HealthLevel.From(100));

            PersonConsistencyRules.ValidateLifeStatusAndSpan(validLife);
        }

        [Fact]
        public void ValidateForDead_WhenEmploymentOrHealthIsInconsistent_ThrowsDomainException()
        {
            var deadLife = LifeState.Create(
                status: LifeStatus.Deceased,
                span: LifeSpan.FromDates(
                    birthDate: new DateOnly(
                        year: 2030,
                        month: 1,
                        day: 1),
                    deathDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 2)),
                health: HealthLevel.From(0));
            var invalidEmployment = EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: PopulationTestData.CreateJob(),
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult);

            Assert.Throws<DomainException>(() => PersonConsistencyRules.ValidateForDead(
                life: deadLife,
                employment: invalidEmployment));
        }

        [Fact]
        public void ValidateEmploymentForAge_WhenCombinationIsConsistent_DoesNotThrow()
        {
            var employment = EmploymentInfo.Create(
                status: EmploymentStatus.Unemployed,
                job: null,
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult);

            PersonConsistencyRules.ValidateEmploymentForAge(
                ageGroup: AgeGroup.Adult,
                employment: employment);
        }

        [Fact]
        public void ValidateEmploymentForAge_WhenChildIsEmployed_ThrowsDomainException()
        {
            var employment = EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: PopulationTestData.CreateJob(),
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult);

            Assert.Throws<DomainException>(() => PersonConsistencyRules.ValidateEmploymentForAge(
                ageGroup: AgeGroup.Child,
                employment: employment));
        }
    }
}
