using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.ValueObjects
{
    public sealed class CityBudgetIdTests
    {
        [Fact]
        public void Constructor_WhenGuidIsValid_PreservesValue()
        {
            var value = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var budgetId = new CityBudgetId(value);

            Assert.Equal(
                expected: value,
                actual: budgetId.Value);
        }

        [Fact]
        public void Constructor_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityBudgetId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
        }

        [Fact]
        public void New_WhenCalled_ReturnsNonEmptyGuid()
        {
            var budgetId = CityBudgetId.New();

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: budgetId.Value);
        }
    }
}
