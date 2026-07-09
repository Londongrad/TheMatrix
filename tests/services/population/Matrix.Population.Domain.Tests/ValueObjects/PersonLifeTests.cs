using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects
{
    public sealed class PersonLifeTests
    {
        [Fact]
        public void PersonNameFromFullName_WhenTwoOrThreePartsProvided_ParsesAndFormatsCorrectly()
        {
            var simple = PersonName.FromFullName("Ivanov Ivan");
            var withPatronymic = PersonName.FromFullName("Ivanov Ivan Ivanovich");

            Assert.Equal(
                expected: "Ivan",
                actual: simple.FirstName);
            Assert.Equal(
                expected: "Ivanov",
                actual: simple.LastName);
            Assert.Null(simple.Patronymic);
            Assert.Equal(
                expected: "Ivanov Ivan",
                actual: simple.ToString());

            Assert.Equal(
                expected: "Ivan",
                actual: withPatronymic.FirstName);
            Assert.Equal(
                expected: "Ivanov",
                actual: withPatronymic.LastName);
            Assert.Equal(
                expected: "Ivanovich",
                actual: withPatronymic.Patronymic);
            Assert.Equal(
                expected: "Ivanov Ivan Ivanovich",
                actual: withPatronymic.ToString());
        }

        [Fact]
        public void PersonNameFromFullName_WhenPartCountIsInvalid_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => PersonName.FromFullName("Ivan"));
            Assert.Throws<DomainException>(() => PersonName.FromFullName("A B C D"));
        }

        [Fact]
        public void AgeFromBirthDateAndAddYears_WhenInputsAreValid_ReturnExpectedYears()
        {
            var age = Age.FromBirthDate(
                birthDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 10),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 9));

            Assert.Equal(
                expected: 17,
                actual: age.Years);
            Assert.Equal(
                expected: 20,
                actual: age.AddYears(3)
                   .Years);
        }

        [Fact]
        public void LifeStateWithHealthDelta_WhenHealthDropsToZero_MarksPersonAsDeceased()
        {
            var lifeState = LifeState.Create(
                status: LifeStatus.Alive,
                span: LifeSpan.FromBirthDate(
                    new DateOnly(
                        year: 2030,
                        month: 1,
                        day: 1)),
                health: HealthLevel.From(10));

            LifeState updated = lifeState.WithHealthDelta(
                delta: -20,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));

            Assert.Equal(
                expected: LifeStatus.Deceased,
                actual: updated.Status);
            Assert.Equal(
                expected: 0,
                actual: updated.Health.Value);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                actual: updated.DeathDate);
        }
    }
}
