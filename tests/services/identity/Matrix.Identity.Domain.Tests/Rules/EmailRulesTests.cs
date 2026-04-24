using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules;

public sealed class EmailRulesTests
{
    [Fact]
    public void Validate_TrimsLowercasesAndReturnsNormalizedEmail()
    {
        var normalizedEmail = EmailRules.Validate("  USER@Example.COM  ");

        Assert.Equal("user@example.com", normalizedEmail);
    }

    [Fact]
    public void Validate_WithWhitespaceEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => EmailRules.Validate("   "));

        Assert.Equal("Identity.User.Email.Empty", exception.Code);
        Assert.Equal("email", exception.PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidFormat_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => EmailRules.Validate("not-an-email"));

        Assert.Equal("Identity.User.Email.InvalidFormat", exception.Code);
        Assert.Equal("email", exception.PropertyName);
    }
}
