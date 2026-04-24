using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class OneTimeTokenTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndStartsActive()
    {
        var token = TokenTestData.CreateOneTimeToken();

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(TokenTestData.UserId, token.UserId);
        Assert.Equal(OneTimeTokenPurpose.PasswordReset, token.Purpose);
        Assert.Equal("one-time-token-hash", token.TokenHash);
        Assert.Equal(TokenTestData.CreatedAtUtc, token.CreatedAtUtc);
        Assert.Equal(TokenTestData.ExpiresAtUtc, token.ExpiresAtUtc);
        Assert.Null(token.UsedAtUtc);
        Assert.Null(token.RevokedAtUtc);
        Assert.True(token.IsActive(TokenTestData.CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void Create_WithInvalidExpiration_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => OneTimeToken.Create(
            userId: TokenTestData.UserId,
            purpose: OneTimeTokenPurpose.EmailConfirmation,
            tokenHash: "token-hash",
            expiresAtUtc: TokenTestData.CreatedAtUtc,
            createdAtUtc: TokenTestData.CreatedAtUtc));

        Assert.Equal("Identity.OneTimeToken.InvalidExpiration", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }

    [Fact]
    public void MarkUsed_WhenTokenIsActive_SetsUsedAtUtc_AndDeactivatesToken()
    {
        var token = TokenTestData.CreateOneTimeToken();
        var usedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(5);

        token.MarkUsed(usedAtUtc);

        Assert.Equal(usedAtUtc, token.UsedAtUtc);
        Assert.False(token.IsActive(usedAtUtc));
    }

    [Fact]
    public void MarkUsed_WhenTokenAlreadyUsed_ThrowsDomainException()
    {
        var token = TokenTestData.CreateOneTimeToken();
        token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(5));

        var exception = Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(6)));

        Assert.Equal("Identity.OneTimeToken.AlreadyUsed", exception.Code);
        Assert.Equal("usedAtUtc", exception.PropertyName);
    }

    [Fact]
    public void MarkUsed_WhenTokenWasRevoked_ThrowsDomainException()
    {
        var token = TokenTestData.CreateOneTimeToken();
        token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(4));

        var exception = Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(5)));

        Assert.Equal("Identity.OneTimeToken.Revoked", exception.Code);
        Assert.Equal("revokedAtUtc", exception.PropertyName);
    }

    [Fact]
    public void MarkUsed_WhenTokenExpired_ThrowsDomainException()
    {
        var token = TokenTestData.CreateOneTimeToken();

        var exception = Assert.Throws<DomainException>(() => token.MarkUsed(TokenTestData.ExpiresAtUtc));

        Assert.Equal("Identity.OneTimeToken.Expired", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }

    [Fact]
    public void Revoke_WhenTokenIsActive_SetsRevokedAtUtc()
    {
        var token = TokenTestData.CreateOneTimeToken();
        var revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(4);

        token.Revoke(revokedAtUtc);

        Assert.Equal(revokedAtUtc, token.RevokedAtUtc);
        Assert.False(token.IsActive(revokedAtUtc));
    }

    [Fact]
    public void Revoke_WhenTokenAlreadyUsed_IsNoOp()
    {
        var token = TokenTestData.CreateOneTimeToken();
        token.MarkUsed(TokenTestData.CreatedAtUtc.AddMinutes(3));

        token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(4));

        Assert.Null(token.RevokedAtUtc);
        Assert.Equal(TokenTestData.CreatedAtUtc.AddMinutes(3), token.UsedAtUtc);
    }

    [Fact]
    public void Revoke_WhenTokenAlreadyRevoked_IsNoOp()
    {
        var token = TokenTestData.CreateOneTimeToken();
        var revokedAtUtc = TokenTestData.CreatedAtUtc.AddMinutes(4);
        token.Revoke(revokedAtUtc);

        token.Revoke(TokenTestData.CreatedAtUtc.AddMinutes(5));

        Assert.Equal(revokedAtUtc, token.RevokedAtUtc);
    }
}
