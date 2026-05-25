using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules
{
    public sealed class EmailRulesTests
    {
        [Fact]
        public void Validate_TrimsLowercasesAndReturnsNormalizedEmail()
        {
            string normalizedEmail = EmailRules.Validate("  USER@Example.COM  ");

            Assert.Equal(
                expected: "user@example.com",
                actual: normalizedEmail);
        }

        [Fact]
        public void Validate_WithWhitespaceEmail_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => EmailRules.Validate("   "));

            Assert.Equal(
                expected: "Identity.User.Email.Empty",
                actual: exception.Code);
            Assert.Equal(
                expected: "email",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Validate_WithInvalidFormat_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => EmailRules.Validate("not-an-email"));

            Assert.Equal(
                expected: "Identity.User.Email.InvalidFormat",
                actual: exception.Code);
            Assert.Equal(
                expected: "email",
                actual: exception.PropertyName);
        }
    }
}
