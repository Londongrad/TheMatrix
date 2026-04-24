using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Create_NormalizesTrimmedLowercaseEmail()
    {
        var email = Email.Create("  USER@Example.COM  ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Create_WithWhitespaceEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Email.Create("   "));

        Assert.Equal("Identity.User.Email.Empty", exception.Code);
        Assert.Equal("email", exception.PropertyName);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Email.Create("user-at-example"));

        Assert.Equal("Identity.User.Email.InvalidFormat", exception.Code);
        Assert.Equal("email", exception.PropertyName);
    }
}
