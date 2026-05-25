using Matrix.BuildingBlocks.Domain.Errors;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests.Errors
{
    public sealed class DomainErrorsFactoryTests
    {
        [Fact]
        public void NullOrEmpty_WhenCreated_UsesExpectedCodeMessageAndProperty()
        {
            DomainException exception = DomainErrorsFactory.NullOrEmpty("Name");

            Assert.Equal(
                expected: "Domain.Guard.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Name",
                actual: exception.PropertyName);
            Assert.Equal(
                expected: "Name cannot be null or empty.",
                actual: exception.Message);
        }

        [Fact]
        public void InvalidDateRange_WhenCreated_UsesExpectedMessageWithoutProperty()
        {
            DomainException exception = DomainErrorsFactory.InvalidDateRange(
                from: new DateOnly(
                    year: 2026,
                    month: 5,
                    day: 3),
                to: new DateOnly(
                    year: 2026,
                    month: 5,
                    day: 2),
                fromName: "from",
                toName: "to");

            Assert.Equal(
                expected: "Domain.Guard.InvalidDateRange",
                actual: exception.Code);
            Assert.Null(exception.PropertyName);
            Assert.Contains(
                expectedSubstring: $"from {new DateOnly(year: 2026, month: 5, day: 3)}",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: $"to {new DateOnly(year: 2026, month: 5, day: 2)}",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void DomainException_WhenConstructedWithBlankCodeAndProperty_NormalizesToDefaults()
        {
            DomainException exception = new(
                code: " ",
                message: "Invalid.",
                propertyName: " ");

            Assert.Equal(
                expected: "Domain.ValidationError",
                actual: exception.Code);
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

            Assert.Contains(
                expectedSubstring: "Population.InvalidAge",
                actualString: text,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "Property: Age",
                actualString: text,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "Age is invalid.",
                actualString: text,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
