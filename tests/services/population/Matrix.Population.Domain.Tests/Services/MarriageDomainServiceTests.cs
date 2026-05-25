using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services
{
    public sealed class MarriageDomainServiceTests
    {
        [Fact]
        public void RegisterMarriage_WhenInputsAreValid_MarksBothPeopleAsMarriedAndUpdatesHappiness()
        {
            var householdId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Person person = PopulationTestData.CreateAdultPerson(
                firstName: "Ivan",
                lastName: "Ivanov",
                sex: Sex.Male,
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: householdId,
                happiness: HappinessLevel.From(50));
            Person spouse = PopulationTestData.CreateAdultPerson(
                firstName: "Anna",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: householdId,
                happiness: HappinessLevel.From(40));
            var service = new MarriageDomainService();

            service.RegisterMarriage(
                person: person,
                spouse: spouse,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: person.MaritalStatus);
            Assert.Equal(
                expected: spouse.Id,
                actual: person.SpouseId);
            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: spouse.MaritalStatus);
            Assert.Equal(
                expected: person.Id,
                actual: spouse.SpouseId);
            Assert.Equal(
                expected: 65,
                actual: person.Happiness.Value);
            Assert.Equal(
                expected: 55,
                actual: spouse.Happiness.Value);
        }

        [Fact]
        public void RegisterMarriage_WhenPersonIsTooYoung_ThrowsDomainException()
        {
            var householdId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Person person = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 2032,
                    month: 6,
                    day: 1));
            Person spouse = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 2030,
                    month: 4,
                    day: 2));
            var service = new MarriageDomainService();

            Assert.Throws<DomainException>(() => service.RegisterMarriage(
                person: person,
                spouse: spouse,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2)));
        }

        [Fact]
        public void RegisterDivorce_WhenPeopleAreMarried_ResetsMaritalStateAndDecreasesHappiness()
        {
            var householdId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var spouseId = PersonId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
            var personId = PersonId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
            Person person = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                personId: personId.Value,
                householdId: householdId,
                happiness: HappinessLevel.From(60),
                maritalStatus: MaritalStatus.Married,
                spouseId: spouseId);
            Person spouse = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                personId: spouseId.Value,
                householdId: householdId,
                happiness: HappinessLevel.From(55),
                maritalStatus: MaritalStatus.Married,
                spouseId: personId);
            var service = new MarriageDomainService();

            service.RegisterDivorce(
                person: person,
                spouse: spouse,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: person.MaritalStatus);
            Assert.Null(person.SpouseId);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: spouse.MaritalStatus);
            Assert.Null(spouse.SpouseId);
            Assert.Equal(
                expected: 45,
                actual: person.Happiness.Value);
            Assert.Equal(
                expected: 40,
                actual: spouse.Happiness.Value);
        }

        [Fact]
        public void RegisterWidowhood_WhenInputsAreValid_MarksWidowAndAppliesHappinessPenalty()
        {
            var householdId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var deceasedId = PersonId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));
            Person widow = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                personId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
                householdId: householdId,
                happiness: HappinessLevel.From(70),
                maritalStatus: MaritalStatus.Married,
                spouseId: deceasedId);
            Person deceased = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                personId: deceasedId.Value,
                householdId: householdId,
                maritalStatus: MaritalStatus.Married,
                spouseId: widow.Id);
            deceased.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));
            var service = new MarriageDomainService();

            service.RegisterWidowhood(
                widow: widow,
                deceased: deceased);

            Assert.Equal(
                expected: MaritalStatus.Widowed,
                actual: widow.MaritalStatus);
            Assert.Null(widow.SpouseId);
            Assert.Equal(
                expected: 40,
                actual: widow.Happiness.Value);
        }
    }
}
