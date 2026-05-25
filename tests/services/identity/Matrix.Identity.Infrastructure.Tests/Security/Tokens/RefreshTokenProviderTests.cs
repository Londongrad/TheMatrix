using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens
{
    public sealed class RefreshTokenProviderTests
    {
        [Fact]
        public void Generate_WhenPersistent_UsesDaysLifetimeFromOptions()
        {
            var provider = new RefreshTokenProvider(
                options: CreateJwtOptions(refreshTokenLifetimeDays: 14),
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            RefreshTokenDescriptor descriptor = provider.Generate(isPersistent: true);

            Assert.Equal(
                expected: CreatedAtUtc.AddDays(14),
                actual: descriptor.ExpiresAtUtc);
            Assert.Equal(
                expected: provider.ComputeHash(descriptor.Token),
                actual: descriptor.TokenHash);
        }

        [Fact]
        public void Generate_WhenShortLived_UsesHoursLifetimeFromOptions()
        {
            var provider = new RefreshTokenProvider(
                options: CreateJwtOptions(shortRefreshTokenLifetimeHours: 6),
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            RefreshTokenDescriptor descriptor = provider.Generate(isPersistent: false);

            Assert.Equal(
                expected: CreatedAtUtc.AddHours(6),
                actual: descriptor.ExpiresAtUtc);
            Assert.Equal(
                expected: provider.ComputeHash(descriptor.Token),
                actual: descriptor.TokenHash);
        }

        [Fact]
        public void ComputeHash_WhenCalledWithSameToken_ReturnsSameHash()
        {
            var provider = new RefreshTokenProvider(
                options: CreateJwtOptions(),
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            string first = provider.ComputeHash("token-value");
            string second = provider.ComputeHash("token-value");

            Assert.Equal(
                expected: first,
                actual: second);
        }
    }
}
