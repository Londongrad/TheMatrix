using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules;

public sealed class OneTimeTokenRulesTests
{
    private static readonly DateTime CreatedAtUtc = new(2046, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAtUtc = CreatedAtUtc.AddMinutes(30);

    [Fact]
    public void ValidateUserId_WithNonEmptyGuid_ReturnsGuid()
    {
        var userId = Guid.Parse("40000000-0000-0000-0000-000000000001");

        var validatedUserId = OneTimeTokenRules.ValidateUserId(userId);

        Assert.Equal(userId, validatedUserId);
    }

    [Fact]
    public void ValidateUserId_WithEmptyGuid_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateUserId(Guid.Empty));

        Assert.Equal("Identity.Common.EmptyId", exception.Code);
        Assert.Equal("userId", exception.PropertyName);
    }

    [Fact]
    public void ValidateTokenHash_TrimsAndReturnsHash()
    {
        var tokenHash = OneTimeTokenRules.ValidateTokenHash("  token-hash  ");

        Assert.Equal("token-hash", tokenHash);
    }

    [Fact]
    public void ValidateTokenHash_WithWhitespaceHash_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateTokenHash("   "));

        Assert.Equal("Identity.OneTimeToken.EmptyTokenHash", exception.Code);
        Assert.Equal("tokenHash", exception.PropertyName);
    }

    [Fact]
    public void ValidatePurpose_WithValidPurpose_ReturnsPurpose()
    {
        var purpose = OneTimeTokenRules.ValidatePurpose(OneTimeTokenPurpose.PasswordReset);

        Assert.Equal(OneTimeTokenPurpose.PasswordReset, purpose);
    }

    [Fact]
    public void ValidatePurpose_WithInvalidPurpose_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidatePurpose((OneTimeTokenPurpose)999));

        Assert.Equal("Identity.OneTimeToken.InvalidPurpose", exception.Code);
        Assert.Equal("purpose", exception.PropertyName);
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
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateExpiration(
            createdAtUtc: CreatedAtUtc,
            expiresAtUtc: CreatedAtUtc));

        Assert.Equal("Identity.OneTimeToken.InvalidExpiration", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }

    [Fact]
    public void ValidateCanBeUsed_WhenTokenWasRevoked_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
            nowUtc: CreatedAtUtc.AddMinutes(5),
            expiresAtUtc: ExpiresAtUtc,
            usedAtUtc: null,
            revokedAtUtc: CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("Identity.OneTimeToken.Revoked", exception.Code);
        Assert.Equal("revokedAtUtc", exception.PropertyName);
    }

    [Fact]
    public void ValidateCanBeUsed_WhenTokenWasAlreadyUsed_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
            nowUtc: CreatedAtUtc.AddMinutes(5),
            expiresAtUtc: ExpiresAtUtc,
            usedAtUtc: CreatedAtUtc.AddMinutes(1),
            revokedAtUtc: null));

        Assert.Equal("Identity.OneTimeToken.AlreadyUsed", exception.Code);
        Assert.Equal("usedAtUtc", exception.PropertyName);
    }

    [Fact]
    public void ValidateCanBeUsed_WhenTokenExpired_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeTokenRules.ValidateCanBeUsed(
            nowUtc: ExpiresAtUtc,
            expiresAtUtc: ExpiresAtUtc,
            usedAtUtc: null,
            revokedAtUtc: null));

        Assert.Equal("Identity.OneTimeToken.Expired", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }
}
