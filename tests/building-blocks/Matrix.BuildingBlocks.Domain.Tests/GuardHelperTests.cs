using Matrix.BuildingBlocks.Domain.Exceptions;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests;

public sealed class GuardHelperTests
{
    [Fact]
    public void AgainstNullOrWhiteSpace_WhenUsingFactoryOverload_TrimsByDefault()
    {
        string value = GuardHelper.AgainstNullOrWhiteSpace(
            value: "  matrix  ",
            errorFactory: propertyName => new DomainException($"invalid {propertyName}"));

        Assert.Equal("matrix", value);
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WhenTrimDisabled_ReturnsOriginalValue()
    {
        string value = GuardHelper.AgainstNullOrWhiteSpace(
            value: "  matrix  ",
            errorFactory: propertyName => new DomainException($"invalid {propertyName}"),
            trim: false);

        Assert.Equal("  matrix  ", value);
    }

    [Fact]
    public void AgainstEmptyGuid_WhenGuidIsEmpty_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GuardHelper.AgainstEmptyGuid(Guid.Empty, "CityId"));

        Assert.Equal("Domain.Guard.EmptyGuid", exception.Code);
        Assert.Equal("CityId", exception.PropertyName);
    }

    [Fact]
    public void AgainstInvalidEnum_WhenValueIsNotDefined_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GuardHelper.AgainstInvalidEnum((TestSupport.TestMode)999, "Mode"));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("Mode", exception.PropertyName);
    }

    [Fact]
    public void AgainstInvalidStringToEnum_WhenValueIsValid_ParsesIgnoringCase()
    {
        TestSupport.TestMode mode = GuardHelper.AgainstInvalidStringToEnum<TestSupport.TestMode>("beta", "Mode");

        Assert.Equal(TestSupport.TestMode.Beta, mode);
    }

    [Fact]
    public void AgainstOutOfRange_WhenValueExceedsBounds_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GuardHelper.AgainstOutOfRange(value: 11, min: 1, max: 10, propertyName: "Intensity"));

        Assert.Equal("Domain.Guard.OutOfRange", exception.Code);
        Assert.Equal("Intensity", exception.PropertyName);
    }

    [Fact]
    public void AgainstInvalidDateRange_WhenFromIsLaterThanTo_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => GuardHelper.AgainstInvalidDateRange(
                from: new DateOnly(2026, 5, 3),
                to: new DateOnly(2026, 5, 2)));

        Assert.Equal("Domain.Guard.InvalidDateRange", exception.Code);
        Assert.Null(exception.PropertyName);
    }
}
