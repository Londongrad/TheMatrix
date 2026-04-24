using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules;

public sealed class RefreshTokenRulesTests
{
    private static readonly DateTime CreatedAtUtc = new(2046, 2, 3, 4, 5, 6, DateTimeKind.Utc);

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
        var exception = Assert.Throws<DomainException>(() => RefreshTokenRules.Validate(
            expiresAtUtc: CreatedAtUtc,
            nowUtc: CreatedAtUtc));

        Assert.Equal("Identity.User.RefreshToken.InvalidExpireDate", exception.Code);
        Assert.Equal("expiresAtUtc", exception.PropertyName);
    }
}
