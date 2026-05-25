using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects
{
    public sealed class UsernameTests
    {
        [Fact]
        public void Create_TrimsAndStoresUsername()
        {
            var username = Username.Create("  matrix  ");

            Assert.Equal(
                expected: "matrix",
                actual: username.Value);
        }

        [Fact]
        public void Create_WithWhitespaceUsername_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Username.Create("   "));

            Assert.Equal(
                expected: "Identity.User.Username.Empty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Username",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenUsernameIsTooShort_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Username.Create("ab"));

            Assert.Equal(
                expected: "Identity.User.Username.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: "Username",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenUsernameIsTooLong_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Username.Create("abcdefghijklmnopq"));

            Assert.Equal(
                expected: "Identity.User.Username.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: "Username",
                actual: exception.PropertyName);
        }
    }
}
