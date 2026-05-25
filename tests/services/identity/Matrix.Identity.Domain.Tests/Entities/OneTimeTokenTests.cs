using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class OneTimeTokenTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndStartsActive()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: token.Id);
            Assert.Equal(
                expected: TokenTestData.UserId,
                actual: token.UserId);
            Assert.Equal(
                expected: OneTimeTokenPurpose.PasswordReset,
                actual: token.Purpose);
            Assert.Equal(
                expected: "one-time-token-hash",
                actual: token.TokenHash);
            Assert.Equal(
                expected: TokenTestData.CreatedAtUtc,
                actual: token.CreatedAtUtc);
            Assert.Equal(
                expected: TokenTestData.ExpiresAtUtc,
                actual: token.ExpiresAtUtc);
            Assert.Null(token.UsedAtUtc);
            Assert.Null(token.RevokedAtUtc);
            Assert.True(token.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
        }

        [Fact]
        public void Create_WithInvalidExpiration_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => OneTimeToken.Create(
                userId: TokenTestData.UserId,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                tokenHash: "token-hash",
                expiresAtUtc: TokenTestData.CreatedAtUtc,
                createdAtUtc: TokenTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.OneTimeToken.InvalidExpiration",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void MarkUsed_WhenTokenIsActive_SetsUsedAtUtc_AndDeactivatesToken()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            DateTime usedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

            token.MarkUsed(usedAtUtc);

            Assert.Equal(
                expected: usedAtUtc,
                actual: token.UsedAtUtc);
            Assert.False(token.IsActive(usedAtUtc));
        }

        [Fact]
        public void MarkUsed_WhenTokenAlreadyUsed_ThrowsDomainException()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(5));

            DomainException exception =
                Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(6)));

            Assert.Equal(
                expected: "Identity.OneTimeToken.AlreadyUsed",
                actual: exception.Code);
            Assert.Equal(
                expected: "usedAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void MarkUsed_WhenTokenWasRevoked_ThrowsDomainException()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(4));

            DomainException exception =
                Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(5)));

            Assert.Equal(
                expected: "Identity.OneTimeToken.Revoked",
                actual: exception.Code);
            Assert.Equal(
                expected: "revokedAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void MarkUsed_WhenTokenExpired_ThrowsDomainException()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();

            DomainException exception =
                Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.ExpiresAtUtc));

            Assert.Equal(
                expected: "Identity.OneTimeToken.Expired",
                actual: exception.Code);
            Assert.Equal(
                expected: "expiresAtUtc",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Revoke_WhenTokenIsActive_SetsRevokedAtUtc()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            DateTime revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(4);

            token.Revoke(revokedAtUtc);

            Assert.Equal(
                expected: revokedAtUtc,
                actual: token.RevokedAtUtc);
            Assert.False(token.IsActive(revokedAtUtc));
        }

        [Fact]
        public void Revoke_WhenTokenAlreadyUsed_IsNoOp()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(3));

            token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(4));

            Assert.Null(token.RevokedAtUtc);
            Assert.Equal(
                expected: TokenTestData.CreatedAtUtc.AddMinutes(3),
                actual: token.UsedAtUtc);
        }

        [Fact]
        public void Revoke_WhenTokenAlreadyRevoked_IsNoOp()
        {
            OneTimeToken token = TokenTestData.CreateOneTimeToken();
            DateTime revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(4);
            token.Revoke(revokedAtUtc);

            token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(5));

            Assert.Equal(
                expected: revokedAtUtc,
                actual: token.RevokedAtUtc);
        }
    }
}
