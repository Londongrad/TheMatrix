using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;
using DomainRefreshToken = Matrix.Identity.Domain.Entities.RefreshToken;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RefreshToken
{
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
            RefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRefreshCommand(refreshToken: "presented-token"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidRefreshToken",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: new[]
                {
                    "presented-token"
                },
                actual: refreshTokenProvider.ComputeHashInputs);
            Assert.Equal(
                expected: refreshTokenProvider.ComputedHash,
                actual: userRepository.RequestedRefreshTokenHash);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(userSessionRepository.Sessions);
            Assert.Null(accessTokenService.RequestedUserId);
        }

        [Theory]
        [InlineData(
            false,
            true,
            "Identity.AccountDeleted",
            RefreshTokenRevocationReason.AccountDeleted)]
        [InlineData(
            true,
            false,
            "Identity.UserBlocked",
            RefreshTokenRevocationReason.UserLocked)]
        public async Task Handle_WhenUserCannotLogin_RevokesActiveTokenAndThrowsExpectedError(
            bool isLocked,
            bool isDeleted,
            string expectedCode,
            RefreshTokenRevocationReason expectedReason)
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(
                isLocked: isLocked,
                isDeleted: isDeleted);
            UserSession session = SelfServiceHandlerTestSupport.CreateSession(user);
            DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: session.Id,
                tokenHash: "incoming-refresh-token-hash");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByRefreshTokenHash = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    session
                }
            };
            RefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                accessTokenService: new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
                refreshTokenProvider: new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
                geoLocationService: new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
                unitOfWork: new SelfServiceHandlerTestSupport.FakeUnitOfWork(),
                permissionsService: new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRefreshCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: expectedCode,
                actual: exception.Code);
            Assert.True(token.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.RevokedAtUtc);
            Assert.Equal(
                expected: expectedReason,
                actual: token.RevokedReason);
        }

        [Fact]
        public async Task Handle_WhenSessionMissing_ThrowsInvalidRefreshToken()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: Guid.NewGuid(),
                tokenHash: "incoming-refresh-token-hash");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByRefreshTokenHash = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            RefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                accessTokenService: new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
                refreshTokenProvider: new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
                geoLocationService: new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
                unitOfWork: unitOfWork,
                permissionsService: new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRefreshCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidRefreshToken",
                actual: exception.Code);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDeviceIdDoesNotMatch_RevokesTokenAndSessionAndThrowsInvalidRefreshToken()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession session = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1");
            DomainRefreshToken token = SelfServiceHandlerTestSupport.SeedRefreshToken(
                user: user,
                sessionId: session.Id,
                tokenHash: "incoming-refresh-token-hash",
                deviceId: "device-1");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByRefreshTokenHash = user
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    session
                }
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            RefreshTokenCommandHandler handler = SelfServiceHandlerTestSupport.CreateRefreshHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                accessTokenService: new SelfServiceHandlerTestSupport.FakeAccessTokenService(),
                refreshTokenProvider: new SelfServiceHandlerTestSupport.FakeRefreshTokenProvider(),
                geoLocationService: new SelfServiceHandlerTestSupport.FakeGeoLocationService(),
                unitOfWork: unitOfWork,
                permissionsService: new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateRefreshCommand(deviceId: "device-2"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidRefreshToken",
                actual: exception.Code);
            Assert.True(token.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SecurityEvent,
                actual: token.RevokedReason);
            Assert.True(session.IsRevoked);
            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: session.RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.SecurityEvent,
                actual: session.RevokedReason);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
