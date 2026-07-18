using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services
{
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
                birthDate: new DateOnly(
                    year: 2026,
                    month: 3,
                    day: 1));
            Person father = PopulationTestData.CreateAdultPerson(
                firstName: "Ivan",
                lastName: "Ivanov",
                sex: Sex.Male,
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: household.Id.Value,
                birthDate: new DateOnly(
                    year: 2024,
                    month: 3,
                    day: 1));
            var newborn = new NewbornProfile(
                PersonId: PersonId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")),
                Name: new PersonName(
                    firstName: "Petr",
                    lastName: "Ivanov"),
                Sex: Sex.Male,
                Personality: Personality.Neutral(),
                Health: HealthLevel.From(95),
                Weight: BodyWeight.FromKilograms(3.6m));
            var service = new PopulationBirthDomainService();

            Person newbornResident = service.RegisterBirth(
                mother: mother,
                father: father,
                household: household,
                newborn: newborn,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.True(newbornResident.IsAlive);
            Assert.Equal(
                expected: household.Id,
                actual: newbornResident.HouseholdId);
            Assert.Equal(
                expected: Sex.Male,
                actual: newbornResident.Sex);
            Assert.Equal(
                expected: EmploymentStatus.None,
                actual: newbornResident.Employment.Status);
            Assert.Equal(
                expected: mother.Id,
                actual: newbornResident.MotherId);
            Assert.Equal(
                expected: father.Id,
                actual: newbornResident.FatherId);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                actual: newbornResident.BirthDate);
            Assert.Equal(
                expected: 4,
                actual: household.Size.Value);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2),
                actual: mother.LastChildbirthDate);
        }

        [Fact]
        public void RegisterBirth_WhenMotherIsNotFemale_ThrowsDomainException()
        {
            Household household = PopulationTestData.CreateHousehold();
            Person mother = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: household.Id.Value,
                birthDate: new DateOnly(
                    year: 2025,
                    month: 4,
                    day: 1));
            var newborn = new NewbornProfile(
                PersonId: PersonId.From(Guid.Parse("55555555-5555-5555-5555-555555555555")),
                Name: new PersonName(
                    firstName: "Petr",
                    lastName: "Ivanov"),
                Sex: Sex.Male,
                Personality: Personality.Neutral(),
                Health: HealthLevel.From(90),
                Weight: BodyWeight.FromKilograms(3.4m));
            var service = new PopulationBirthDomainService();

            Assert.Throws<DomainException>(() => service.RegisterBirth(
                mother: mother,
                father: null,
                household: household,
                newborn: newborn,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2)));
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
                birthDate: new DateOnly(
                    year: 2025,
                    month: 4,
                    day: 1));
            var firstNewborn = new NewbornProfile(
                PersonId: PersonId.From(Guid.Parse("77777777-7777-7777-7777-777777777777")),
                Name: new PersonName(
                    firstName: "Petr",
                    lastName: "Ivanov"),
                Sex: Sex.Male,
                Personality: Personality.Neutral(),
                Health: HealthLevel.From(91),
                Weight: BodyWeight.FromKilograms(3.5m));
            var secondNewborn = new NewbornProfile(
                PersonId: PersonId.From(Guid.Parse("88888888-8888-8888-8888-888888888888")),
                Name: new PersonName(
                    firstName: "Maria",
                    lastName: "Ivanova"),
                Sex: Sex.Female,
                Personality: Personality.Neutral(),
                Health: HealthLevel.From(92),
                Weight: BodyWeight.FromKilograms(3.2m));
            var service = new PopulationBirthDomainService();
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 2);

            service.RegisterBirth(
                mother: mother,
                father: null,
                household: household,
                newborn: firstNewborn,
                currentDate: currentDate);

            Assert.Throws<DomainException>(() => service.RegisterBirth(
                mother: mother,
                father: null,
                household: household,
                newborn: secondNewborn,
                currentDate: currentDate));
        }
    }
}
