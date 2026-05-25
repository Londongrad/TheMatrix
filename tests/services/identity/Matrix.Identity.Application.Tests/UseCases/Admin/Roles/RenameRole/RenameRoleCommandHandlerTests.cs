using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.RenameRole
{
    public sealed class RenameRoleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleExists_RenamesAndPersistsIt()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            roleWriteRepository.RolesById[role.Id] = role;
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new RenameRoleCommandHandler(
                roleReadRepository: roleReadRepository,
                roleWriteRepository: roleWriteRepository,
                unitOfWork: unitOfWork);

            RoleRenamedResult result = await handler.Handle(
                request: new RenameRoleCommand(
                    RoleId: role.Id,
                    Name: "  Moderators  "),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: role.Id,
                actual: roleWriteRepository.RequestedRoleId);
            Assert.Equal(
                expected: ("  Moderators  ", role.Id),
                actual: roleReadRepository.ExistsByNameExceptRequests.Single());
            Assert.Equal(
                expected: "Moderators",
                actual: role.Name);
            Assert.Equal(
                expected: role.Id,
                actual: result.Id);
            Assert.Equal(
                expected: "Moderators",
                actual: result.Name);
            Assert.False(result.IsSystem);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
        {
            var handler = new RenameRoleCommandHandler(
                roleReadRepository: new AdminRolesTestSupport.FakeRoleReadRepository(),
                roleWriteRepository: new AdminRolesTestSupport.FakeRoleWriteRepository(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RenameRoleCommand(
                        RoleId: Guid.NewGuid(),
                        Name: "Moderators"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
        {
            Role role = AdminRolesTestSupport.CreateRole(
                name: "SuperAdmin",
                isSystem: true);
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            roleWriteRepository.RolesById[role.Id] = role;
            var handler = new RenameRoleCommandHandler(
                roleReadRepository: new AdminRolesTestSupport.FakeRoleReadRepository(),
                roleWriteRepository: roleWriteRepository,
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RenameRoleCommand(
                        RoleId: role.Id,
                        Name: "Operators"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.ReadOnly",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenNameAlreadyInUse_ThrowsConflict()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.ExistingNames.Add("Moderators");
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            roleWriteRepository.RolesById[role.Id] = role;
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new RenameRoleCommandHandler(
                roleReadRepository: roleReadRepository,
                roleWriteRepository: roleWriteRepository,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RenameRoleCommand(
                        RoleId: role.Id,
                        Name: "Moderators"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.Name.AlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: "Operators",
                actual: role.Name);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
