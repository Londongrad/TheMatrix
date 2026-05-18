using Matrix.BuildingBlocks.Domain.Errors;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests.Errors;

public sealed class DomainErrorsFactoryTests
{
    [Fact]
    public void NullOrEmpty_WhenCreated_UsesExpectedCodeMessageAndProperty()
    {
        DomainException exception = DomainErrorsFactory.NullOrEmpty("Name");

        Assert.Equal("Domain.Guard.NullOrEmpty", exception.Code);
        Assert.Equal("Name", exception.PropertyName);
        Assert.Equal("Name cannot be null or empty.", exception.Message);
    }

    [Fact]
    public void InvalidDateRange_WhenCreated_UsesExpectedMessageWithoutProperty()
    {
        DomainException exception = DomainErrorsFactory.InvalidDateRange(
            from: new DateOnly(2026, 5, 3),
            to: new DateOnly(2026, 5, 2),
            fromName: "from",
            toName: "to");

        Assert.Equal("Domain.Guard.InvalidDateRange", exception.Code);
        Assert.Null(exception.PropertyName);
        Assert.Contains($"from {new DateOnly(2026, 5, 3)}", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"to {new DateOnly(2026, 5, 2)}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DomainException_WhenConstructedWithBlankCodeAndProperty_NormalizesToDefaults()
    {
        DomainException exception = new(code: " ", message: "Invalid.", propertyName: " ");

        Assert.Equal("Domain.ValidationError", exception.Code);
        Assert.Null(exception.PropertyName);
    }

    [Fact]
    public void DomainException_ToString_IncludesCodePropertyAndMessage()
    {
        DomainException exception = new(
            code: "Population.InvalidAge",
            message: "Age is invalid.",
            propertyName: "Age");

        string text = exception.ToString();

        Assert.Contains("Population.InvalidAge", text, StringComparison.Ordinal);
        Assert.Contains("Property: Age", text, StringComparison.Ordinal);
        Assert.Contains("Age is invalid.", text, StringComparison.Ordinal);
    }
}
