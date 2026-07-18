using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects
{
    public sealed class EmploymentAndMaritalTests
    {
        [Fact]
        public void AgeGroupRules_WhenBoundaryAgesAreProvided_ReturnsExpectedGroup()
        {
            Assert.Equal(
                expected: AgeGroup.Child,
                actual: AgeGroupRules.GetAgeGroup(Age.FromYears(6)));
            Assert.Equal(
                expected: AgeGroup.Youth,
                actual: AgeGroupRules.GetAgeGroup(Age.FromYears(7)));
            Assert.Equal(
                expected: AgeGroup.Adult,
                actual: AgeGroupRules.GetAgeGroup(Age.FromYears(18)));
            Assert.Equal(
                expected: AgeGroup.Senior,
                actual: AgeGroupRules.GetAgeGroup(Age.FromYears(66)));
        }

        [Fact]
        public void EmploymentInfoCreate_WhenAdultIsEmployedWithJob_Succeeds()
        {
            var employment = EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                    title: "Engineer"),
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult);

            Assert.Equal(
                expected: EmploymentStatus.Employed,
                actual: employment.Status);
            Assert.NotNull(employment.Job);
            Assert.Equal(
                expected: "Engineer",
                actual: employment.Job!.Title);
        }

        [Fact]
        public void EmploymentInfoCreate_WhenCombinationIsInvalid_ThrowsDomainException()
        {
            Assert.Throws<DomainException>(() => EmploymentInfo.Create(
                status: EmploymentStatus.Employed,
                job: null,
                lifeStatus: LifeStatus.Alive,
                ageGroup: AgeGroup.Adult));

            Assert.Throws<DomainException>(() => EmploymentInfo.Create(
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
            var spouseId = PersonId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));

            var married = MaritalInfo.MarriedWith(spouseId);
            var single = MaritalInfo.Single();

            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: married.Status);
            Assert.Equal(
                expected: spouseId,
                actual: married.SpouseId);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: single.Status);
            Assert.Null(single.SpouseId);
        }

        [Fact]
        public void MaritalInfoFromStatus_WhenSpouseCombinationIsInvalid_ThrowsDomainException()
        {
            var spouseId = PersonId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));

            Assert.Throws<DomainException>(() => MaritalInfo.FromStatus(
                status: MaritalStatus.Single,
                spouseId: spouseId));
            Assert.Throws<DomainException>(() => MaritalInfo.FromStatus(
                status: MaritalStatus.Married,
                spouseId: null));
        }
    }
}
