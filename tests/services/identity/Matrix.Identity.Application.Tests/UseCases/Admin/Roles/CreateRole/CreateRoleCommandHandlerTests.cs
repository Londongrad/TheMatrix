using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.CreateRole;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleNameIsAvailable_CreatesRoleAndPersistsIt()
    {
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new CreateRoleCommandHandler(
            roleReadRepository,
            roleWriteRepository,
            AdminRolesTestSupport.CreateTimeProvider(),
            unitOfWork);

        var result = await handler.Handle(
            new CreateRoleCommand("  Operators  "),
            CancellationToken.None);

        var persistedRole = Assert.Single(roleWriteRepository.AddedRoles);
        Assert.Equal("Operators", persistedRole.Name);
        Assert.False(persistedRole.IsSystem);
        Assert.Equal(AdminRolesTestSupport.UtcNow, persistedRole.CreatedAtUtc);
        Assert.Equal("Operators", roleReadRepository.ExistsByNameRequests.Single());
        Assert.Equal(persistedRole.Id, result.Id);
        Assert.Equal("Operators", result.Name);
        Assert.False(result.IsSystem);
        Assert.Equal(AdminRolesTestSupport.UtcNow, result.CreatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenRoleNameAlreadyExists_ThrowsConflict()
    {
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.ExistingNames.Add("Operators");
        var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new CreateRoleCommandHandler(
            roleReadRepository,
            roleWriteRepository,
            AdminRolesTestSupport.CreateTimeProvider(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new CreateRoleCommand("Operators"),
            CancellationToken.None));

        Assert.Equal("Identity.Role.Name.AlreadyInUse", exception.Code);
        Assert.Equal(ApplicationErrorType.Conflict, exception.ErrorType);
        Assert.Empty(roleWriteRepository.AddedRoles);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }
}
