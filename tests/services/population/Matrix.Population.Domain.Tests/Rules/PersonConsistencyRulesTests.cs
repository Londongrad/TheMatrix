using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Rules;

public sealed class PersonConsistencyRulesTests
{
    [Fact]
    public void ValidateLifeStatusAndSpan_WhenStateIsValid_DoesNotThrow()
    {
        LifeState validLife = LifeState.Create(
            status: LifeStatus.Alive,
            span: LifeSpan.FromDates(
                birthDate: new DateOnly(2030, 1, 1),
                deathDate: null),
            health: HealthLevel.From(100));

        PersonConsistencyRules.ValidateLifeStatusAndSpan(validLife);
    }

    [Fact]
    public void ValidateForDead_WhenEmploymentOrHealthIsInconsistent_ThrowsDomainException()
    {
        LifeState deadLife = LifeState.Create(
            status: LifeStatus.Deceased,
            span: LifeSpan.FromDates(
                birthDate: new DateOnly(2030, 1, 1),
                deathDate: new DateOnly(2048, 5, 2)),
            health: HealthLevel.From(0));
        EmploymentInfo invalidEmployment = EmploymentInfo.Create(
            status: EmploymentStatus.Employed,
            job: PopulationTestData.CreateJob(),
            lifeStatus: LifeStatus.Alive,
            ageGroup: AgeGroup.Adult);

        Assert.Throws<DomainException>(() => PersonConsistencyRules.ValidateForDead(deadLife, invalidEmployment));
    }

    [Fact]
    public void ValidateEmploymentForAge_WhenCombinationIsConsistent_DoesNotThrow()
    {
        EmploymentInfo employment = EmploymentInfo.Create(
            status: EmploymentStatus.Student,
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
        EmploymentInfo employment = EmploymentInfo.Create(
            status: EmploymentStatus.Employed,
            job: PopulationTestData.CreateJob(),
            lifeStatus: LifeStatus.Alive,
            ageGroup: AgeGroup.Adult);

        Assert.Throws<DomainException>(
            () => PersonConsistencyRules.ValidateEmploymentForAge(
                ageGroup: AgeGroup.Child,
                employment: employment));
    }
}
