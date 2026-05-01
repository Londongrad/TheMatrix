using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects;

public sealed class EducationEmploymentAndMaritalTests
{
    [Fact]
    public void AgeGroupRules_WhenBoundaryAgesAreProvided_ReturnsExpectedGroup()
    {
        Assert.Equal(AgeGroup.Child, AgeGroupRules.GetAgeGroup(Age.FromYears(6)));
        Assert.Equal(AgeGroup.Youth, AgeGroupRules.GetAgeGroup(Age.FromYears(7)));
        Assert.Equal(AgeGroup.Adult, AgeGroupRules.GetAgeGroup(Age.FromYears(18)));
        Assert.Equal(AgeGroup.Senior, AgeGroupRules.GetAgeGroup(Age.FromYears(66)));
    }

    [Fact]
    public void EducationInfoGraduateTo_WhenTransitionIsValid_UpdatesLevel()
    {
        EducationInfo education = EducationInfo.FromLevel(EducationLevel.UpperSecondary);

        EducationInfo graduated = education.GraduateTo(EducationLevel.Higher);

        Assert.Equal(EducationLevel.Higher, graduated.Level);
    }

    [Fact]
    public void EducationInfoGraduateTo_WhenTransitionIsInvalid_ThrowsDomainException()
    {
        EducationInfo education = EducationInfo.FromLevel(EducationLevel.Higher);
        EducationInfo postgraduate = EducationInfo.FromLevel(EducationLevel.Postgraduate);

        Assert.Throws<DomainException>(() => education.GraduateTo(EducationLevel.Primary));
        Assert.Throws<DomainException>(() => postgraduate.GraduateTo(EducationLevel.Higher));
    }

    [Fact]
    public void EmploymentInfoCreate_WhenAdultIsEmployedWithJob_Succeeds()
    {
        EmploymentInfo employment = EmploymentInfo.Create(
            status: EmploymentStatus.Employed,
            job: new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                title: "Engineer"),
            lifeStatus: LifeStatus.Alive,
            ageGroup: AgeGroup.Adult);

        Assert.Equal(EmploymentStatus.Employed, employment.Status);
        Assert.NotNull(employment.Job);
        Assert.Equal("Engineer", employment.Job!.Title);
    }

    [Fact]
    public void EmploymentInfoCreate_WhenCombinationIsInvalid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: null,
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult));

        Assert.Throws<DomainException>(
            () => EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")),
                    title: "Cashier"),
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Child));
    }

    [Fact]
    public void MaritalInfoFactoryMethods_WhenUsedWithValidInputs_ReturnExpectedState()
    {
        PersonId spouseId = PersonId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

        MaritalInfo married = MaritalInfo.MarriedWith(spouseId);
        MaritalInfo single = MaritalInfo.Single();

        Assert.Equal(MaritalStatus.Married, married.Status);
        Assert.Equal(spouseId, married.SpouseId);
        Assert.Equal(MaritalStatus.Single, single.Status);
        Assert.Null(single.SpouseId);
    }

    [Fact]
    public void MaritalInfoFromStatus_WhenSpouseCombinationIsInvalid_ThrowsDomainException()
    {
        PersonId spouseId = PersonId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));

        Assert.Throws<DomainException>(() => MaritalInfo.FromStatus(MaritalStatus.Single, spouseId));
        Assert.Throws<DomainException>(() => MaritalInfo.FromStatus(MaritalStatus.Married, null));
    }
}
