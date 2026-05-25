using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects
{
    public sealed class EmailTests
    {
        [Fact]
        public void Create_NormalizesTrimmedLowercaseEmail()
        {
            var email = Email.Create("  USER@Example.COM  ");

            Assert.Equal(
                expected: "user@example.com",
                actual: email.Value);
        }

        [Fact]
        public void Create_WithWhitespaceEmail_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Email.Create("   "));

            Assert.Equal(
                expected: "Identity.User.Email.Empty",
                actual: exception.Code);
            Assert.Equal(
                expected: "email",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidEmail_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Email.Create("user-at-example"));

            Assert.Equal(
                expected: "Identity.User.Email.InvalidFormat",
                actual: exception.Code);
            Assert.Equal(
                expected: "email",
                actual: exception.PropertyName);
        }
    }
}
