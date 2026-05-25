using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.CreateRole
{
    public sealed class CreateRoleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleNameIsAvailable_CreatesRoleAndPersistsIt()
        {
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new CreateRoleCommandHandler(
                roleReadRepository: roleReadRepository,
                roleWriteRepository: roleWriteRepository,
                timeProvider: AdminRolesTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork);

            RoleCreatedResult result = await handler.Handle(
                request: new CreateRoleCommand("  Operators  "),
                cancellationToken: CancellationToken.None);

            Role persistedRole = Assert.Single(roleWriteRepository.AddedRoles);
            Assert.Equal(
                expected: "Operators",
                actual: persistedRole.Name);
            Assert.False(persistedRole.IsSystem);
            Assert.Equal(
                expected: AdminRolesTestSupport.UtcNow,
                actual: persistedRole.CreatedAtUtc);
            Assert.Equal(
                expected: "Operators",
                actual: roleReadRepository.ExistsByNameRequests.Single());
            Assert.Equal(
                expected: persistedRole.Id,
                actual: result.Id);
            Assert.Equal(
                expected: "Operators",
                actual: result.Name);
            Assert.False(result.IsSystem);
            Assert.Equal(
                expected: AdminRolesTestSupport.UtcNow,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenRoleNameAlreadyExists_ThrowsConflict()
        {
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.ExistingNames.Add("Operators");
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new CreateRoleCommandHandler(
                roleReadRepository: roleReadRepository,
                roleWriteRepository: roleWriteRepository,
                timeProvider: AdminRolesTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new CreateRoleCommand("Operators"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.Name.AlreadyInUse",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Empty(roleWriteRepository.AddedRoles);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
