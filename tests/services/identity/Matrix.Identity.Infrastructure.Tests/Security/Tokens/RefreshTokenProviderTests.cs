using Matrix.Identity.Infrastructure.Security.Tokens;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens;

public sealed class RefreshTokenProviderTests
{
    [Fact]
    public void Generate_WhenPersistent_UsesDaysLifetimeFromOptions()
    {
        var provider = new RefreshTokenProvider(
            options: CreateJwtOptions(refreshTokenLifetimeDays: 14),
            clock: CreateClock(CreatedAtUtc));

        var descriptor = provider.Generate(isPersistent: true);

        Assert.Equal(CreatedAtUtc.AddDays(14), descriptor.ExpiresAtUtc);
        Assert.Equal(provider.ComputeHash(descriptor.Token), descriptor.TokenHash);
    }

    [Fact]
    public void Generate_WhenShortLived_UsesHoursLifetimeFromOptions()
    {
        var provider = new RefreshTokenProvider(
            options: CreateJwtOptions(shortRefreshTokenLifetimeHours: 6),
            clock: CreateClock(CreatedAtUtc));

        var descriptor = provider.Generate(isPersistent: false);

        Assert.Equal(CreatedAtUtc.AddHours(6), descriptor.ExpiresAtUtc);
        Assert.Equal(provider.ComputeHash(descriptor.Token), descriptor.TokenHash);
    }

    [Fact]
    public void ComputeHash_WhenCalledWithSameToken_ReturnsSameHash()
    {
        var provider = new RefreshTokenProvider(
            options: CreateJwtOptions(),
            clock: CreateClock(CreatedAtUtc));

        string first = provider.ComputeHash("token-value");
        string second = provider.ComputeHash("token-value");

        Assert.Equal(first, second);
    }
}
