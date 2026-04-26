using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Admin.Users.RestoreUser;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.RestoreUser;

public sealed class RestoreUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.NewGuid()
        };
        var handler = new RestoreUserCommandHandler(
            new AdminUsersTestSupport.FakeUserRepository(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminUsersTestSupport.FakeSecurityAuditService(),
            new AdminUsersTestSupport.FakeEmailSender(),
            new AdminRolesTestSupport.FakeUnitOfWork(),
            currentUser,
            NullLogger<RestoreUserCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RestoreUserCommand(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenUserIsDeleted_RestoresUserWritesAuditAndSendsEmail()
    {
        var user = AdminUsersTestSupport.CreateUser(isDeleted: true);
        var adminUserId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            UserById = user
        };
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var securityAuditService = new AdminUsersTestSupport.FakeSecurityAuditService();
        var emailSender = new AdminUsersTestSupport.FakeEmailSender();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = adminUserId
        };
        var handler = new RestoreUserCommandHandler(
            userRepository,
            adminUserGuard,
            securityStateChangeCollector,
            securityAuditService,
            emailSender,
            unitOfWork,
            currentUser,
            NullLogger<RestoreUserCommandHandler>.Instance);

        await handler.Handle(new RestoreUserCommand(user.Id), CancellationToken.None);

        Assert.False(user.IsDeleted);
        Assert.Equal(2, user.PermissionsVersion);
        Assert.Equal(user.Id, adminUserGuard.RequestedTargetUserId);
        Assert.Equal([user.Id], securityStateChangeCollector.ChangedUsers);
        var auditEntry = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountRestored, auditEntry.EventType);
        Assert.Equal(user.Id, auditEntry.UserId);
        Assert.Equal($"RestoredBy:{adminUserId:D}", auditEntry.Details);
        Assert.Equal(["neo@matrix.local"], emailSender.AccountRestoredEmails);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotDeleted_DoesNothingAfterTransaction()
    {
        var user = AdminUsersTestSupport.CreateUser(isDeleted: false);
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var securityAuditService = new AdminUsersTestSupport.FakeSecurityAuditService();
        var emailSender = new AdminUsersTestSupport.FakeEmailSender();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new RestoreUserCommandHandler(
            new AdminUsersTestSupport.FakeUserRepository { UserById = user },
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            securityStateChangeCollector,
            securityAuditService,
            emailSender,
            unitOfWork,
            new AdminUsersTestSupport.FakeCurrentUserContext { UserId = Guid.NewGuid() },
            NullLogger<RestoreUserCommandHandler>.Instance);

        await handler.Handle(new RestoreUserCommand(user.Id), CancellationToken.None);

        Assert.Empty(securityStateChangeCollector.ChangedUsers);
        Assert.Empty(securityAuditService.Entries);
        Assert.Empty(emailSender.AccountRestoredEmails);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
