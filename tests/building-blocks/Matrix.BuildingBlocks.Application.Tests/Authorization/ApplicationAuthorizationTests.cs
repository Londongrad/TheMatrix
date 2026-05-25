using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Authorization
{
    public sealed class ApplicationAuthorizationTests
    {
        [Fact]
        public void CurrentUserContextExtensions_WhenAuthenticated_ReturnIds()
        {
            var userId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            TestCurrentUserContext context = new()
            {
                IsAuthenticated = true,
                UserId = userId,
                SessionId = sessionId
            };

            Assert.Equal(
                expected: userId,
                actual: context.GetUserIdOrThrow());
            Assert.Equal(
                expected: sessionId,
                actual: context.GetSessionIdOrThrow());
        }

        [Fact]
        public void CurrentUserContextExtensions_WhenUnauthenticated_ThrowUnauthorized()
        {
            TestCurrentUserContext context = new()
            {
                IsAuthenticated = false
            };

            MatrixApplicationException userException =
                Assert.Throws<MatrixApplicationException>(() => context.GetUserIdOrThrow());
            MatrixApplicationException sessionException =
                Assert.Throws<MatrixApplicationException>(() => context.GetSessionIdOrThrow());

            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: userException.ErrorType);
            Assert.Equal(
                expected: "Common.Unauthorized",
                actual: userException.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: sessionException.ErrorType);
            Assert.Equal(
                expected: "Common.Unauthorized",
                actual: sessionException.Code);
        }

        [Fact]
        public void InternalJwtSigningKeyPolicy_WhenKeyIsStrong_ValidatesSuccessfully()
        {
            bool valid = InternalJwtSigningKeyPolicy.TryValidate(
                signingKey: "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
                validationError: out string? validationError);

            Assert.True(valid);
            Assert.Null(validationError);
        }

        [Fact]
        public void InternalJwtSigningKeyPolicy_WhenKeyIsWeak_ReturnsValidationError()
        {
            bool valid = InternalJwtSigningKeyPolicy.TryValidate(
                signingKey: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                validationError: out string? validationError);

            Assert.False(valid);
            Assert.Contains(
                expectedSubstring: "distinct characters",
                actualString: validationError,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public void InternalJwtKeyRingPolicy_WhenNoKeyRingConfigured_FallsBackToLegacySigningKey()
        {
            TestInternalJwtKeyRingOptions options = new()
            {
                SigningKey = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&"
            };

            InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(
                options: options,
                optionsPath: "InternalJwt");

            Assert.Equal(
                expected: InternalJwtKeyRingPolicy.LegacyKeyId,
                actual: keyRing.CurrentKeyId);
            Assert.Equal(
                expected: options.SigningKey,
                actual: keyRing.CurrentSigningKey);
            Assert.True(keyRing.Keys.ContainsKey(InternalJwtKeyRingPolicy.LegacyKeyId));
        }

        [Fact]
        public void InternalJwtKeyRingPolicy_WhenConfiguredKeysExist_UsesCurrentKeyId()
        {
            TestInternalJwtKeyRingOptions options = new()
            {
                CurrentKeyId = "current",
                Keys = new Dictionary<string, string>
                {
                    ["current"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
                    ["next"] = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$"
                }
            };

            InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(
                options: options,
                optionsPath: "InternalJwt");

            Assert.Equal(
                expected: "current",
                actual: keyRing.CurrentKeyId);
            Assert.Equal(
                expected: options.Keys["current"],
                actual: keyRing.CurrentSigningKey);
            Assert.Equal(
                expected: 2,
                actual: keyRing.Keys.Count);
        }

        [Fact]
        public void InternalJwtKeyRingPolicy_WhenCurrentKeyIdIsMissing_Throws()
        {
            TestInternalJwtKeyRingOptions options = new()
            {
                Keys = new Dictionary<string, string>
                {
                    ["current"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&"
                }
            };

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => InternalJwtKeyRingPolicy.Resolve(
                    options: options,
                    optionsPath: "InternalJwt"));

            Assert.Contains(
                expectedSubstring: "CurrentKeyId is required",
                actualString: exception.Message,
                comparisonType: StringComparison.Ordinal);
        }

        private sealed class TestInternalJwtKeyRingOptions : IInternalJwtKeyRingOptions
        {
            public string Issuer { get; init; } = "https://gateway.test";
            public string Audience { get; init; } = "internal-clients";
            public string SigningKey { get; init; } = string.Empty;
            public int LifetimeSeconds { get; init; } = 300;
            public string? CurrentKeyId { get; init; }
            public IDictionary<string, string>? Keys { get; init; }
        }
    }
}
