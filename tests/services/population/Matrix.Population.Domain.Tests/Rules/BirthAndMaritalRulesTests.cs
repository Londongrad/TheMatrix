using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Rules
{
    public sealed class BirthAndMaritalRulesTests
    {
        [Fact]
        public void ValidateBirth_WhenParentsDoNotShareHousehold_ThrowsDomainException()
        {
            Household motherHousehold = PopulationTestData.CreateHousehold();
            Household fatherHousehold = PopulationTestData.CreateHousehold();
            fatherHousehold.Resize(HouseholdSize.From(2));
            Person mother = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                householdId: motherHousehold.Id.Value,
                birthDate: new DateOnly(
                    year: 2025,
                    month: 1,
                    day: 1));
            Person father = PopulationTestData.CreateAdultPerson(
                sex: Sex.Male,
                householdId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
                birthDate: new DateOnly(
                    year: 2024,
                    month: 1,
                    day: 1));

            Action act = () => BirthRules.ValidateBirth(
                mother: mother,
                father: father,
                household: motherHousehold,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Throws<DomainException>(act);
        }

        [Fact]
        public void ValidateBirth_WhenHouseholdIsFull_ThrowsDomainException()
        {
            Household household = PopulationTestData.CreateHousehold();
            household.Resize(HouseholdSize.From(HouseholdSize.Max));
            Person mother = PopulationTestData.CreateAdultPerson(
                sex: Sex.Female,
                householdId: household.Id.Value,
                birthDate: new DateOnly(
                    year: 2025,
                    month: 1,
                    day: 1));

            Action act = () => BirthRules.ValidateBirth(
                mother: mother,
                father: null,
                household: household,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Throws<DomainException>(act);
        }

        [Fact]
        public void ValidateNewMarriage_WhenSpouseIsAlreadyMarried_ThrowsDomainException()
        {
            var personId = PersonId.From(Guid.Parse("11111111-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var spouseId = PersonId.From(Guid.Parse("22222222-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            Assert.Throws<DomainException>(() => MaritalRules.ValidateNewMarriage(
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
            var personId = PersonId.From(Guid.Parse("33333333-cccc-cccc-cccc-cccccccccccc"));

            Assert.Throws<DomainException>(() => MaritalRules.ValidateDivorce(
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
            var widowId = PersonId.From(Guid.Parse("44444444-dddd-dddd-dddd-dddddddddddd"));
            var deceasedId = PersonId.From(Guid.Parse("55555555-eeee-eeee-eeee-eeeeeeeeeeee"));

            Assert.Throws<DomainException>(() => MaritalRules.ValidateWidowhood(
                widowId: widowId,
                widowLifeStatus: LifeStatus.Alive,
                widowMarital: MaritalInfo.Single(),
                deceasedId: deceasedId,
                deceasedLifeStatus: LifeStatus.Deceased,
                deceasedMarital: MaritalInfo.MarriedWith(widowId)));
        }
    }
}
