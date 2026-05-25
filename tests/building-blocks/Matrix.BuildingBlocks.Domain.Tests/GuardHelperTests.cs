using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.BuildingBlocks.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests
{
    public sealed class GuardHelperTests
    {
        [Fact]
        public void AgainstNullOrWhiteSpace_WhenUsingFactoryOverload_TrimsByDefault()
        {
            string value = GuardHelper.AgainstNullOrWhiteSpace(
                value: "  matrix  ",
                errorFactory: propertyName => new DomainException($"invalid {propertyName}"));

            Assert.Equal(
                expected: "matrix",
                actual: value);
        }

        [Fact]
        public void AgainstNullOrWhiteSpace_WhenTrimDisabled_ReturnsOriginalValue()
        {
            string value = GuardHelper.AgainstNullOrWhiteSpace(
                value: "  matrix  ",
                errorFactory: propertyName => new DomainException($"invalid {propertyName}"),
                trim: false);

            Assert.Equal(
                expected: "  matrix  ",
                actual: value);
        }

        [Fact]
        public void AgainstEmptyGuid_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(()
                => GuardHelper.AgainstEmptyGuid(
                    id: Guid.Empty,
                    propertyName: "CityId"));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "CityId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void AgainstInvalidEnum_WhenValueIsNotDefined_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(()
                => GuardHelper.AgainstInvalidEnum(
                    value: (TestMode)999,
                    propertyName: "Mode"));

            Assert.Equal(
                expected: "Domain.Guard.InvalidEnum",
                actual: exception.Code);
            Assert.Equal(
                expected: "Mode",
                actual: exception.PropertyName);
        }

        [Fact]
        public void AgainstInvalidStringToEnum_WhenValueIsValid_ParsesIgnoringCase()
        {
            TestMode mode = GuardHelper.AgainstInvalidStringToEnum<TestMode>(
                value: "beta",
                propertyName: "Mode");

            Assert.Equal(
                expected: TestMode.Beta,
                actual: mode);
        }

        [Fact]
        public void AgainstOutOfRange_WhenValueExceedsBounds_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => GuardHelper.AgainstOutOfRange(
                value: 11,
                min: 1,
                max: 10,
                propertyName: "Intensity"));

            Assert.Equal(
                expected: "Domain.Guard.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "Intensity",
                actual: exception.PropertyName);
        }

        [Fact]
        public void AgainstInvalidDateRange_WhenFromIsLaterThanTo_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => GuardHelper.AgainstInvalidDateRange(
                from: new DateOnly(
                    year: 2026,
                    month: 5,
                    day: 3),
                to: new DateOnly(
                    year: 2026,
                    month: 5,
                    day: 2)));

            Assert.Equal(
                expected: "Domain.Guard.InvalidDateRange",
                actual: exception.Code);
            Assert.Null(exception.PropertyName);
        }
    }
}
