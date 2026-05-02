using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services;

public sealed class PopulationBirthDomainServiceTests
{
    [Fact]
    public void RegisterBirth_WhenInputsAreValid_CreatesNewbornResizesHouseholdAndMarksMother()
    {
        Household household = PopulationTestData.CreateHousehold(cashReserve: 200m);
        Person mother = PopulationTestData.CreateAdultPerson(
            firstName: "Anna",
            lastName: "Ivanova",
            sex: Sex.Female,
            personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2026, 3, 1));
        Person father = PopulationTestData.CreateAdultPerson(
            firstName: "Ivan",
            lastName: "Ivanov",
            sex: Sex.Male,
            personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2024, 3, 1));
        var newborn = new NewbornProfile(
            PersonId: Matrix.Population.Domain.ValueObjects.PersonId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            Name: new Matrix.Population.Domain.ValueObjects.PersonName("Petr", "Ivanov"),
            Sex: Sex.Male,
            Personality: Matrix.Population.Domain.ValueObjects.Personality.Neutral(),
            Health: Matrix.Population.Domain.ValueObjects.HealthLevel.From(95),
            Weight: Matrix.Population.Domain.ValueObjects.BodyWeight.FromKilograms(3.6m));
        var service = new PopulationBirthDomainService();

        Person newbornResident = service.RegisterBirth(
            mother: mother,
            father: father,
            household: household,
            newborn: newborn,
            currentDate: new DateOnly(2048, 5, 2));

        Assert.True(newbornResident.IsAlive);
        Assert.Equal(household.Id, newbornResident.HouseholdId);
        Assert.Equal(Sex.Male, newbornResident.Sex);
        Assert.Equal(EducationLevel.None, newbornResident.EducationLevel);
        Assert.Equal(EmploymentStatus.None, newbornResident.Employment.Status);
        Assert.Equal(mother.Id, newbornResident.MotherId);
        Assert.Equal(father.Id, newbornResident.FatherId);
        Assert.Equal(new DateOnly(2048, 5, 2), newbornResident.BirthDate);
        Assert.Equal(4, household.Size.Value);
        Assert.Equal(new DateOnly(2048, 5, 2), mother.LastChildbirthDate);
    }

    [Fact]
    public void RegisterBirth_WhenMotherIsNotFemale_ThrowsDomainException()
    {
        Household household = PopulationTestData.CreateHousehold();
        Person mother = PopulationTestData.CreateAdultPerson(
            sex: Sex.Male,
            personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2025, 4, 1));
        var newborn = new NewbornProfile(
            PersonId: Matrix.Population.Domain.ValueObjects.PersonId.From(Guid.Parse("55555555-5555-5555-5555-555555555555")),
            Name: new Matrix.Population.Domain.ValueObjects.PersonName("Petr", "Ivanov"),
            Sex: Sex.Male,
            Personality: Matrix.Population.Domain.ValueObjects.Personality.Neutral(),
            Health: Matrix.Population.Domain.ValueObjects.HealthLevel.From(90),
            Weight: Matrix.Population.Domain.ValueObjects.BodyWeight.FromKilograms(3.4m));
        var service = new PopulationBirthDomainService();

        Assert.Throws<DomainException>(
            () => service.RegisterBirth(
                mother: mother,
                father: null,
                household: household,
                newborn: newborn,
                currentDate: new DateOnly(2048, 5, 2)));
    }

    [Fact]
    public void RegisterBirth_WhenMotherAlreadyGaveBirthOnCurrentDate_ThrowsDomainException()
    {
        Household household = PopulationTestData.CreateHousehold();
        Person mother = PopulationTestData.CreateAdultPerson(
            firstName: "Anna",
            lastName: "Ivanova",
            sex: Sex.Female,
            personId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            householdId: household.Id.Value,
            birthDate: new DateOnly(2025, 4, 1));
        var firstNewborn = new NewbornProfile(
            PersonId: Matrix.Population.Domain.ValueObjects.PersonId.From(Guid.Parse("77777777-7777-7777-7777-777777777777")),
            Name: new Matrix.Population.Domain.ValueObjects.PersonName("Petr", "Ivanov"),
            Sex: Sex.Male,
            Personality: Matrix.Population.Domain.ValueObjects.Personality.Neutral(),
            Health: Matrix.Population.Domain.ValueObjects.HealthLevel.From(91),
            Weight: Matrix.Population.Domain.ValueObjects.BodyWeight.FromKilograms(3.5m));
        var secondNewborn = new NewbornProfile(
            PersonId: Matrix.Population.Domain.ValueObjects.PersonId.From(Guid.Parse("88888888-8888-8888-8888-888888888888")),
            Name: new Matrix.Population.Domain.ValueObjects.PersonName("Maria", "Ivanova"),
            Sex: Sex.Female,
            Personality: Matrix.Population.Domain.ValueObjects.Personality.Neutral(),
            Health: Matrix.Population.Domain.ValueObjects.HealthLevel.From(92),
            Weight: Matrix.Population.Domain.ValueObjects.BodyWeight.FromKilograms(3.2m));
        var service = new PopulationBirthDomainService();
        DateOnly currentDate = new(2048, 5, 2);

        service.RegisterBirth(
            mother: mother,
            father: null,
            household: household,
            newborn: firstNewborn,
            currentDate: currentDate);

        Assert.Throws<DomainException>(
            () => service.RegisterBirth(
                mother: mother,
                father: null,
                household: household,
                newborn: secondNewborn,
                currentDate: currentDate));
    }
}
