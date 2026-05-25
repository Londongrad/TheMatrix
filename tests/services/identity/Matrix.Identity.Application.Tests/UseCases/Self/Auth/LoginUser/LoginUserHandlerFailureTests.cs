using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser
{
    public sealed class LoginUserHandlerFailureTests
    {
        [Fact]
        public async Task Handle_WhenLoginNotAllowed_WritesAuditAndThrowsTooManyAttempts()
        {
            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository();
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher();
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService();
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService
            {
                IsLoginAllowedResult = false
            };
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: LoginUserHandlerTestSupport.CreateCommand(login: " Neo@Matrix.Local "),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Auth.TooManyAttempts",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.TooManyRequests,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.Login,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: audit.Subject);
            Assert.Equal(
                expected: "RateLimitExceeded",
                actual: audit.Details);
            Assert.Null(audit.UserId);
            Assert.Null(audit.SessionId);
            Assert.Null(userRepository.RequestedEmail);
            Assert.Null(userRepository.RequestedUsername);
        }

        [Fact]
        public async Task Handle_WhenUserNotFound_WritesAuditAndThrowsInvalidCredentials()
        {
            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository();
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher();
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService();
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService();
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: LoginUserHandlerTestSupport.CreateCommand(login: " Neo "),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidCredentials",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "Neo",
                actual: userRepository.RequestedUsername);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "Neo",
                actual: audit.Subject);
            Assert.Equal(
                expected: "UserNotFound",
                actual: audit.Details);
            Assert.Null(audit.UserId);
        }

        [Fact]
        public async Task Handle_WhenPasswordInvalid_WritesAuditAndThrowsInvalidCredentials()
        {
            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository
            {
                UserByEmail = LoginUserHandlerTestSupport.CreateUser()
            };
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher
            {
                VerifyOutcome = PasswordVerificationOutcome.Failed
            };
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService();
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService();
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: LoginUserHandlerTestSupport.CreateCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidCredentials",
                actual: exception.Code);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: "InvalidPassword",
                actual: audit.Details);
            Assert.Equal(
                expected: userRepository.UserByEmail!.Id,
                actual: audit.UserId);
            Assert.Null(audit.SessionId);
        }

        [Theory]
        [InlineData(
            false,
            true,
            "Identity.AccountDeleted",
            ApplicationErrorType.Forbidden,
            "AccountDeleted")]
        [InlineData(
            true,
            false,
            "Identity.UserBlocked",
            ApplicationErrorType.Forbidden,
            "UserBlocked")]
        public async Task Handle_WhenUserCannotLogin_WritesAuditAndThrowsExpectedError(
            bool isLocked,
            bool isDeleted,
            string expectedCode,
            ApplicationErrorType expectedErrorType,
            string expectedDetails)
        {
            var userRepository = new LoginUserHandlerTestSupport.FakeUserRepository
            {
                UserByEmail = LoginUserHandlerTestSupport.CreateUser(
                    isLocked: isLocked,
                    isDeleted: isDeleted)
            };
            var userSessionRepository = new LoginUserHandlerTestSupport.FakeUserSessionRepository();
            var passwordHasher = new LoginUserHandlerTestSupport.FakePasswordHasher();
            var accessTokenService = new LoginUserHandlerTestSupport.FakeAccessTokenService();
            var refreshTokenProvider = new LoginUserHandlerTestSupport.FakeRefreshTokenProvider();
            var geoLocationService = new LoginUserHandlerTestSupport.FakeGeoLocationService();
            var unitOfWork = new LoginUserHandlerTestSupport.FakeUnitOfWork();
            var permissionsService = new LoginUserHandlerTestSupport.FakeEffectivePermissionsService();
            var securityAuditService = new LoginUserHandlerTestSupport.FakeSecurityAuditService();
            LoginUserCommandHandler handler = LoginUserHandlerTestSupport.CreateHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                passwordHasher: passwordHasher,
                accessTokenService: accessTokenService,
                refreshTokenProvider: refreshTokenProvider,
                geoLocationService: geoLocationService,
                unitOfWork: unitOfWork,
                permissionsService: permissionsService,
                securityAuditService: securityAuditService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: LoginUserHandlerTestSupport.CreateCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: expectedCode,
                actual: exception.Code);
            Assert.Equal(
                expected: expectedErrorType,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: expectedDetails,
                actual: audit.Details);
            Assert.Equal(
                expected: userRepository.UserByEmail!.Id,
                actual: audit.UserId);
        }
    }
}
