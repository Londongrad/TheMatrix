using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens
{
    public sealed class OneTimeTokenServiceTests
    {
        [Fact]
        public void HashToken_TrimsWhitespaceAndReturnsStableHash()
        {
            var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

            string trimmed = service.HashToken("token-value");
            string padded = service.HashToken("  token-value  ");

            Assert.Equal(
                expected: trimmed,
                actual: padded);
        }

        [Fact]
        public void GetPolicyValues_WhenPurposeIsPasswordReset_ReturnsConfiguredSettings()
        {
            var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

            TimeSpan ttl = service.GetTtl(OneTimeTokenPurpose.PasswordReset);
            TimeSpan cooldown = service.GetDeliveryCooldown(OneTimeTokenPurpose.PasswordReset);
            int maxAttempts = service.GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose.PasswordReset);

            Assert.Equal(
                expected: TimeSpan.FromMinutes(60),
                actual: ttl);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(60),
                actual: cooldown);
            Assert.Equal(
                expected: 5,
                actual: maxAttempts);
        }

        [Fact]
        public void GetTtl_WhenPurposeIsUnsupported_ThrowsArgumentOutOfRangeException()
        {
            var service = new OneTimeTokenService(CreateOneTimeTokenOptions());

            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetTtl((OneTimeTokenPurpose)999));
        }
    }
}
