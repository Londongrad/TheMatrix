using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens;

public sealed class OneTimeTokenServiceTests
{
    [Fact]
    public void HashToken_TrimsWhitespaceAndReturnsStableHash()
    {
        var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

        string trimmed = service.HashToken("token-value");
        string padded = service.HashToken("  token-value  ");

        Assert.Equal(trimmed, padded);
    }

    [Fact]
    public void GetPolicyValues_WhenPurposeIsPasswordReset_ReturnsConfiguredSettings()
    {
        var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

        TimeSpan ttl = service.GetTtl(OneTimeTokenPurpose.PasswordReset);
        TimeSpan cooldown = service.GetDeliveryCooldown(OneTimeTokenPurpose.PasswordReset);
        int maxAttempts = service.GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose.PasswordReset);

        Assert.Equal(TimeSpan.FromMinutes(60), ttl);
        Assert.Equal(TimeSpan.FromSeconds(60), cooldown);
        Assert.Equal(5, maxAttempts);
    }

    [Fact]
    public void GetTtl_WhenPurposeIsUnsupported_ThrowsArgumentOutOfRangeException()
    {
        var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetTtl((OneTimeTokenPurpose)999));
    }
}
