using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandlerFailureTests
{
    [Fact]
    public async Task Handle_WhenRefreshTokenUnknown_ThrowsInvalidRefreshToken()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var accessTokenService = new SelfServiceHandlerTestSupport.FakeAccessTokenService();
        var refreshTokenProvider = new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider();
        var geoLocationService = new SelfServiceHandlerTestSupport.FakeGeoLocationService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService();
        var handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
            userRepository,
            userSessionRepository,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRefreshCommand(refreshToken: "presented-token"),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidRefreshToken", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Equal(new[] { "presented-token" }, refreshTokenProvider.ComputeHashInputs);
        Assert.Equal(refreshTokenProvider.ComputedHash, userRepository.RequestedRefreshTokenHash);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(userSessionRepository.Sessions);
        Assert.Null(accessTokenService.RequestedUserId);
    }

    [Theory]
    [InlineData(false, true, "Identity.AccountDeleted", RefreshTokenRevocationReason.AccountDeleted)]
    [InlineData(true, false, "Identity.UserBlocked", RefreshTokenRevocationReason.UserLocked)]
    public async Task Handle_WhenUserCannotLogin_RevokesActiveTokenAndThrowsExpectedError(
        bool isLocked,
        bool isDeleted,
        string expectedCode,
        RefreshTokenRevocationReason expectedReason)
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(
            isLocked: isLocked,
            isDeleted: isDeleted);
        var session = SelfServiceHandlerTestSupport.CreateSession(user);
        DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: session.Id,
            tokenHash: "incoming-refresh-token-hash");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByRefreshTokenHash = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { session }
        };
        var handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
            new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
            new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
            new SelfServiceHandlerTestSupport.FakeUnitOfWork(),
            new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRefreshCommand(),
            CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
        Assert.True(token.IsRevoked);
        Assert.Equal(expectedReason, token.RevokedReason);
    }

    [Fact]
    public async Task Handle_WhenSessionMissing_ThrowsInvalidRefreshToken()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: Guid.NewGuid(),
            tokenHash: "incoming-refresh-token-hash");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByRefreshTokenHash = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
            new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
            new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRefreshCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidRefreshToken", exception.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDeviceIdDoesNotMatch_RevokesTokenAndSessionAndThrowsInvalidRefreshToken()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var session = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
            user,
            sessionId: session.Id,
            tokenHash: "incoming-refresh-token-hash",
            deviceId: "device-1");
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByRefreshTokenHash = user
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { session }
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
            new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
            new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateRefreshCommand(deviceId: "device-2"),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidRefreshToken", exception.Code);
        Assert.True(token.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.SecurityEvent, token.RevokedReason);
        Assert.True(session.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.SecurityEvent, session.RevokedReason);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
