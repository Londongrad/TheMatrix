using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects;

public sealed class UsernameTests
{
    [Fact]
    public void Create_TrimsAndStoresUsername()
    {
        var username = Username.Create("  matrix  ");

        Assert.Equal("matrix", username.Value);
    }

    [Fact]
    public void Create_WithWhitespaceUsername_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Username.Create("   "));

        Assert.Equal("Identity.User.Username.Empty", exception.Code);
        Assert.Equal("Username", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenUsernameIsTooShort_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Username.Create("ab"));

        Assert.Equal("Identity.User.Username.InvalidLength", exception.Code);
        Assert.Equal("Username", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenUsernameIsTooLong_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Username.Create("abcdefghijklmnopq"));

        Assert.Equal("Identity.User.Username.InvalidLength", exception.Code);
        Assert.Equal("Username", exception.PropertyName);
    }
}
