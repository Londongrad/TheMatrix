using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Rules;

public sealed class BirthAndMaritalRulesTests
{
    [Fact]
    public void ValidateBirth_WhenParentsDoNotShareHousehold_ThrowsDomainException()
    {
        Household motherHousehold = PopulationTestData.CreateHousehold();
        Household fatherHousehold = PopulationTestData.CreateHousehold();
        fatherHousehold.Resize(HouseholdSize.From(2));
        var mother = PopulationTestData.CreateAdultPerson(
            sex: Sex.Female,
            householdId: motherHousehold.Id.Value,
            birthDate: new DateOnly(2025, 1, 1));
        var father = PopulationTestData.CreateAdultPerson(
            sex: Sex.Male,
            householdId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
            birthDate: new DateOnly(2024, 1, 1));

        Action act = () => BirthRules.ValidateBirth(
            mother: mother,
            father: father,
            household: motherHousehold,
            currentDate: new DateOnly(2048, 5, 2));

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void ValidateBirth_WhenHouseholdIsFull_ThrowsDomainException()
    {
        Household household = PopulationTestData.CreateHousehold();
        household.Resize(HouseholdSize.From(HouseholdSize.Max));
        var mother = PopulationTestData.CreateAdultPerson(
            sex: Sex.Female,
            householdId: household.Id.Value,
            birthDate: new DateOnly(2025, 1, 1));

        Action act = () => BirthRules.ValidateBirth(
            mother: mother,
            father: null,
            household: household,
            currentDate: new DateOnly(2048, 5, 2));

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void ValidateNewMarriage_WhenSpouseIsAlreadyMarried_ThrowsDomainException()
    {
        PersonId personId = PersonId.From(Guid.Parse("11111111-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        PersonId spouseId = PersonId.From(Guid.Parse("22222222-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        Assert.Throws<DomainException>(
            () => MaritalRules.ValidateNewMarriage(
                personId: personId,
                personAge: Age.FromYears(25),
                personLifeStatus: LifeStatus.Alive,
                personMarital: MaritalInfo.Single(),
                spouseId: spouseId,
                spouseAge: Age.FromYears(24),
                spouceLifeStatus: LifeStatus.Alive,
                spouseMarital: MaritalInfo.MarriedWith(personId)));
    }

    [Fact]
    public void ValidateDivorce_WhenPersonDivorcesSelf_ThrowsDomainException()
    {
        PersonId personId = PersonId.From(Guid.Parse("33333333-cccc-cccc-cccc-cccccccccccc"));

        Assert.Throws<DomainException>(
            () => MaritalRules.ValidateDivorce(
                personId: personId,
                personLifeStatus: LifeStatus.Alive,
                personMarital: MaritalInfo.MarriedWith(personId),
                spouseId: personId,
                spouceLifeStatus: LifeStatus.Alive,
                spouseMarital: MaritalInfo.MarriedWith(personId)));
    }

    [Fact]
    public void ValidateWidowhood_WhenWidowIsNotMarried_ThrowsDomainException()
    {
        PersonId widowId = PersonId.From(Guid.Parse("44444444-dddd-dddd-dddd-dddddddddddd"));
        PersonId deceasedId = PersonId.From(Guid.Parse("55555555-eeee-eeee-eeee-eeeeeeeeeeee"));

        Assert.Throws<DomainException>(
            () => MaritalRules.ValidateWidowhood(
                widowId: widowId,
                widowLifeStatus: LifeStatus.Alive,
                widowMarital: MaritalInfo.Single(),
                deceasedId: deceasedId,
                deceasedLifeStatus: LifeStatus.Deceased,
                deceasedMarital: MaritalInfo.MarriedWith(widowId)));
    }
}
