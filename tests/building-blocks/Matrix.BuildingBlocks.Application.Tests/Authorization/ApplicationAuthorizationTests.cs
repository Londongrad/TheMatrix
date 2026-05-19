using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Authorization;

public sealed class ApplicationAuthorizationTests
{
    [Fact]
    public void CurrentUserContextExtensions_WhenAuthenticated_ReturnIds()
    {
        Guid userId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        TestCurrentUserContext context = new()
        {
            IsAuthenticated = true,
            UserId = userId,
            SessionId = sessionId
        };

        Assert.Equal(userId, context.GetUserIdOrThrow());
        Assert.Equal(sessionId, context.GetSessionIdOrThrow());
    }

    [Fact]
    public void CurrentUserContextExtensions_WhenUnauthenticated_ThrowUnauthorized()
    {
        TestCurrentUserContext context = new()
        {
            IsAuthenticated = false
        };

        MatrixApplicationException userException = Assert.Throws<MatrixApplicationException>(() => context.GetUserIdOrThrow());
        MatrixApplicationException sessionException = Assert.Throws<MatrixApplicationException>(() => context.GetSessionIdOrThrow());

        Assert.Equal(ApplicationErrorType.Unauthorized, userException.ErrorType);
        Assert.Equal("Common.Unauthorized", userException.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, sessionException.ErrorType);
        Assert.Equal("Common.Unauthorized", sessionException.Code);
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
        Assert.Contains("distinct characters", validationError, StringComparison.Ordinal);
    }

    [Fact]
    public void InternalJwtKeyRingPolicy_WhenNoKeyRingConfigured_FallsBackToLegacySigningKey()
    {
        TestInternalJwtKeyRingOptions options = new()
        {
            SigningKey = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&"
        };

        InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(options, "InternalJwt");

        Assert.Equal(InternalJwtKeyRingPolicy.LegacyKeyId, keyRing.CurrentKeyId);
        Assert.Equal(options.SigningKey, keyRing.CurrentSigningKey);
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

        InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(options, "InternalJwt");

        Assert.Equal("current", keyRing.CurrentKeyId);
        Assert.Equal(options.Keys["current"], keyRing.CurrentSigningKey);
        Assert.Equal(2, keyRing.Keys.Count);
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

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => InternalJwtKeyRingPolicy.Resolve(options, "InternalJwt"));

        Assert.Contains("CurrentKeyId is required", exception.Message, StringComparison.Ordinal);
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
