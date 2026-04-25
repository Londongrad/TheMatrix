using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.RenameRole;

public sealed class RenameRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_RenamesAndPersistsIt()
    {
        var role = AdminRolesTestSupport.CreateRole("Operators");
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        roleWriteRepository.RolesById[role.Id] = role;
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new RenameRoleCommandHandler(
            roleReadRepository,
            roleWriteRepository,
            unitOfWork);

        var result = await handler.Handle(
            new RenameRoleCommand(role.Id, "  Moderators  "),
            CancellationToken.None);

        Assert.Equal(role.Id, roleWriteRepository.RequestedRoleId);
        Assert.Equal(("  Moderators  ", role.Id), roleReadRepository.ExistsByNameExceptRequests.Single());
        Assert.Equal("Moderators", role.Name);
        Assert.Equal(role.Id, result.Id);
        Assert.Equal("Moderators", result.Name);
        Assert.False(result.IsSystem);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var handler = new RenameRoleCommandHandler(
            new AdminRolesTestSupport.FakeRoleReadRepository(),
            new AdminRolesTestSupport.FakeRoleWriteRepository(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RenameRoleCommand(Guid.NewGuid(), "Moderators"),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
    {
        var role = AdminRolesTestSupport.CreateRole("SuperAdmin", isSystem: true);
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        roleWriteRepository.RolesById[role.Id] = role;
        var handler = new RenameRoleCommandHandler(
            new AdminRolesTestSupport.FakeRoleReadRepository(),
            roleWriteRepository,
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RenameRoleCommand(role.Id, "Operators"),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.ReadOnly", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyInUse_ThrowsConflict()
    {
        var role = AdminRolesTestSupport.CreateRole("Operators");
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.ExistingNames.Add("Moderators");
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        roleWriteRepository.RolesById[role.Id] = role;
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new RenameRoleCommandHandler(
            roleReadRepository,
            roleWriteRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new RenameRoleCommand(role.Id, "Moderators"),
            CancellationToken.None));

        Assert.Equal("Identity.Role.Name.AlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Equal("Operators", role.Name);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }
}
