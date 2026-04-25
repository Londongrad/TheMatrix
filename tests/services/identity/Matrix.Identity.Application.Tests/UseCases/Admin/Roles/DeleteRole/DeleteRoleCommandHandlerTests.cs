using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_DeletesRoleAndMarksAffectedUsers()
    {
        var role = AdminRolesTestSupport.CreateRole("Operators");
        Guid firstUserId = Guid.NewGuid();
        Guid secondUserId = Guid.NewGuid();
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        roleWriteRepository.RolesById[role.Id] = role;
        var userRepository = new AdminRolesTestSupport.FakeUserRepository();
        userRepository.UserIdsByRoleId[role.Id] = new[] { firstUserId, secondUserId };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new DeleteRoleCommandHandler(
            roleWriteRepository,
            userRepository,
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        Assert.Equal(role.Id, roleWriteRepository.RequestedRoleId);
        Assert.Equal(role.Id, userRepository.RequestedRoleId);
        Assert.Equal(new[] { firstUserId, secondUserId }, securityStateChangeCollector.ChangedUsers);
        Assert.Same(role, Assert.Single(roleWriteRepository.DeletedRoles));
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new DeleteRoleCommandHandler(
            roleWriteRepository,
            new AdminRolesTestSupport.FakeUserRepository(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new DeleteRoleCommand(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(0, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
    {
        var role = AdminRolesTestSupport.CreateRole("SuperAdmin", isSystem: true);
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        roleWriteRepository.RolesById[role.Id] = role;
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new DeleteRoleCommandHandler(
            roleWriteRepository,
            new AdminRolesTestSupport.FakeUserRepository(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new DeleteRoleCommand(role.Id),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.ReadOnly", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
        Assert.Empty(roleWriteRepository.DeletedRoles);
        Assert.Equal(0, unitOfWork.TransactionCalls);
    }
}
