using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeUsername;

public sealed class ChangeUsernameCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("90000000-0000-0000-0000-000000000002")
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            userRepository,
            passwordHasher,
            securityAuditService,
            emailSender,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            currentUser,
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Empty(passwordHasher.VerifyCalls);
        Assert.Empty(securityAuditService.Entries);
        Assert.Empty(emailSender.UsernameChangedEmails);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenUsernameSame_ReturnsExistingUsernameWithoutSideEffects()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            passwordHasher,
            securityAuditService,
            emailSender,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        string result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo"),
            CancellationToken.None);

        Assert.Equal("neo", result);
        Assert.Empty(passwordHasher.VerifyCalls);
        Assert.Empty(securityAuditService.Entries);
        Assert.Empty(emailSender.UsernameChangedEmails);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_WritesAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(username: "neo", passwordHash: "stored-hash");
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
        {
            VerifyOutcome = PasswordVerificationOutcome.Failed
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            passwordHasher,
            securityAuditService,
            new SelfServiceHandlerTestSupport.FakeEmailSender(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-prime", currentPassword: "WrongPa$$w0rd"),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCurrentPassword", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Single(passwordHasher.VerifyCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.UsernameChanged, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("neo-prime", audit.Subject);
        Assert.Equal("InvalidCurrentPassword", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenCooldownActive_WritesAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
        user.ChangeUsername(
            username: Username.Create("neo-prime"),
            changedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddDays(-1));
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            passwordHasher,
            securityAuditService,
            new SelfServiceHandlerTestSupport.FakeEmailSender(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-ultimate"),
            CancellationToken.None));

        Assert.Equal("Identity.UsernameChangeCooldown", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
        Assert.Single(passwordHasher.VerifyCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("neo-ultimate", audit.Subject);
        Assert.Equal(
            $"CooldownUntil:{SelfServiceHandlerTestSupport.UtcNow.AddDays(-1).AddDays(30):O}",
            audit.Details);
    }

    [Fact]
    public async Task Handle_WhenUsernameTaken_WritesAuditAndThrowsConflict()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserById = user,
            IsUsernameTakenAsyncResult = true
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            userRepository,
            passwordHasher,
            securityAuditService,
            new SelfServiceHandlerTestSupport.FakeEmailSender(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-prime"),
            CancellationToken.None));

        Assert.Equal("Identity.UsernameAlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Equal("neo-prime", userRepository.RequestedUsername);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal("neo-prime", audit.Subject);
        Assert.Equal("UsernameAlreadyInUse", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenRequestValid_ChangesUsernameWritesAuditAndSendsEmail()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(username: "neo");
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserById = user
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler(
            userRepository,
            passwordHasher,
            securityAuditService,
            emailSender,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername.ChangeUsernameCommandHandler>.Instance);

        string result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeUsernameCommand(username: "neo-prime"),
            CancellationToken.None);

        Assert.Equal("neo-prime", result);
        Assert.Equal("neo-prime", user.Username.Value);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, user.LastUsernameChangedAtUtc);
        Assert.Equal("neo-prime", userRepository.RequestedUsername);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.UsernameChanged, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("neo-prime", audit.Subject);
        Assert.Equal("PreviousUsername:neo", audit.Details);

        var email = Assert.Single(emailSender.UsernameChangedEmails);
        Assert.Equal(user.Email.Value, email.ToEmail);
        Assert.Equal("neo", email.PreviousUsername);
        Assert.Equal("neo-prime", email.NewUsername);
    }
}
