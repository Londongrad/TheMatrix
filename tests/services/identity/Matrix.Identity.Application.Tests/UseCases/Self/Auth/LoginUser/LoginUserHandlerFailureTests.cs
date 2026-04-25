using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser;

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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(login: " Neo@Matrix.Local "),
            CancellationToken.None));

        Assert.Equal("Identity.Auth.TooManyAttempts", exception.Code);
        Assert.Equal(ApplicationErrorType.TooManyRequests, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.Login, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("neo@matrix.local", audit.Subject);
        Assert.Equal("RateLimitExceeded", audit.Details);
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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(login: " Neo "),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCredentials", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Equal("Neo", userRepository.RequestedUsername);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("Neo", audit.Subject);
        Assert.Equal("UserNotFound", audit.Details);
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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCredentials", exception.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("InvalidPassword", audit.Details);
        Assert.Equal(userRepository.UserByEmail!.Id, audit.UserId);
        Assert.Null(audit.SessionId);
    }

    [Theory]
    [InlineData(false, true, "Identity.AccountDeleted", ApplicationErrorType.Forbidden, "AccountDeleted")]
    [InlineData(true, false, "Identity.UserBlocked", ApplicationErrorType.Forbidden, "UserBlocked")]
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
        var handler = LoginUserHandlerTestSupport.CreateHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            LoginUserHandlerTestSupport.CreateCommand(),
            CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedErrorType, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        var audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(expectedDetails, audit.Details);
        Assert.Equal(userRepository.UserByEmail!.Id, audit.UserId);
    }
}
