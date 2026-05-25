using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules
{
    public sealed class RefreshTokenRulesTests
    {
        private static readonly DateTime CreatedAtUtc = new(
            year: 2046,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            kind: DateTimeKind.Utc);

        [Fact]
        public void Validate_WhenExpirationIsInTheFuture_Succeeds()
        {
            RefreshTokenRules.Validate(
                expiresAtUtc: CreatedAtUtc.AddMinutes(30),
                nowUtc: CreatedAtUtc);
        }

        [Fact]
        public void Validate_WhenExpirationIsNotInTheFuture_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RefreshTokenRules.Validate(
                expiresAtUtc: CreatedAtUtc,
                nowUtc: CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.User.RefreshToken.InvalidExpireDate",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }
    }
}
