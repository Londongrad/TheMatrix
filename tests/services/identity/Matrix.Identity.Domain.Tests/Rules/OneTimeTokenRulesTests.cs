using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules
{
    public sealed class OneTimeTokenRulesTests
    {
        private static readonly DateTime CreatedAtUtc = new(
            year: 2046,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            kind: DateTimeKind.Utc);

        private static readonly DateTime ExpiresAtUtc = CreatedAtUtc.AddMinutes(30);

        [Fact]
        public void ValidateUserId_WithNonEmptyGuid_ReturnsGuid()
        {
            var userId = Guid.Parse("40000000-0000-0000-0000-000000000001");

            Guid validatedUserId = OneTimeTokenRules.ValidateUserId(userId);

            Assert.Equal(
                expected: userId,
                actual: validatedUserId);
        }

        [Fact]
        public void ValidateUserId_WithEmptyGuid_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateUserId(Guid.Empty));

            Assert.Equal(
                expected: "Identity.Common.EmptyId",
                actual: exception.Code);
            Assert.Equal(
                expected: "userId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidateTokenHash_TrimsAndReturnsHash()
        {
            string tokenHash = OneTimeTokenRules.ValidateTokenHash("  token-hash  ");

            Assert.Equal(
                expected: "token-hash",
                actual: tokenHash);
        }

        [Fact]
        public void ValidateTokenHash_WithWhitespaceHash_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateTokenHash("   "));

            Assert.Equal(
                expected: "Identity.OneTimeToken.EmptyTokenHash",
                actual: exception.Code);
            Assert.Equal(
                expected: "tokenHash",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidatePurpose_WithValidPurpose_ReturnsPurpose()
        {
            OneTimeTokenPurpose purpose = OneTimeTokenRules.ValidatePurpose(OneTimeTokenPurpose.PasswordReset);

            Assert.Equal(
                expected: OneTimeTokenPurpose.PasswordReset,
                actual: purpose);
        }

        [Fact]
        public void ValidatePurpose_WithInvalidPurpose_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidatePurpose((OneTimeTokenPurpose)999));

            Assert.Equal(
                expected: "Identity.OneTimeToken.InvalidPurpose",
                actual: exception.Code);
            Assert.Equal(
                expected: "purpose",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidateExpiration_WhenExpiresAfterCreatedAt_Succeeds()
        {
            OneTimeTokenRules.ValidateExpiration(
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: ExpiresAtUtc);
        }

        [Fact]
        public void ValidateExpiration_WhenExpiresAtOrBeforeCreatedAt_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateExpiration(
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.OneTimeToken.InvalidExpiration",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidateCanBeUsed_WhenTokenWasRevoked_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
                nowUtc: CreatedAtUtc.AddMinutes(5),
                expiresAtUtc: ExpiresAtUtc,
                usedAtUtc: null,
                revokedAtUtc: CreatedAtUtc.AddMinutes(1)));

            Assert.Equal(
                expected: "Identity.OneTimeToken.Revoked",
                actual: exception.Code);
            Assert.Equal(
                expected: "revokedAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidateCanBeUsed_WhenTokenWasAlreadyUsed_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
                nowUtc: CreatedAtUtc.AddMinutes(5),
                expiresAtUtc: ExpiresAtUtc,
                usedAtUtc: CreatedAtUtc.AddMinutes(1),
                revokedAtUtc: null));

            Assert.Equal(
                expected: "Identity.OneTimeToken.AlreadyUsed",
                actual: exception.Code);
            Assert.Equal(
                expected: "usedAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ValidateCanBeUsed_WhenTokenExpired_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
                nowUtc: ExpiresAtUtc,
                expiresAtUtc: ExpiresAtUtc,
                usedAtUtc: null,
                revokedAtUtc: null));

            Assert.Equal(
                expected: "Identity.OneTimeToken.Expired",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }
    }
}
