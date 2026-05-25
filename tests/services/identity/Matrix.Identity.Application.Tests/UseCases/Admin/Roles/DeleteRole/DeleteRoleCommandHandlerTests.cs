using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.DeleteRole
{
    public sealed class DeleteRoleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleExists_DeletesRoleAndMarksAffectedUsers()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            roleWriteRepository.RolesById[role.Id] = role;
            var userRepository = new AdminRolesTestSupport.FakeUserRepository();
            userRepository.UserIdsByRoleId[role.Id] = new[]
            {
                firstUserId,
                secondUserId
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new DeleteRoleCommandHandler(
                roleWriteRepository: roleWriteRepository,
                userRepository: userRepository,
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new DeleteRoleCommand(role.Id),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: role.Id,
                actual: roleWriteRepository.RequestedRoleId);
            Assert.Equal(
                expected: role.Id,
                actual: userRepository.RequestedRoleId);
            Assert.Equal(
                expected: new[]
                {
                    firstUserId,
                    secondUserId
                },
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Same(
                expected: role,
                actual: Assert.Single(roleWriteRepository.DeletedRoles));
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
        {
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new DeleteRoleCommandHandler(
                roleWriteRepository: roleWriteRepository,
                userRepository: new AdminRolesTestSupport.FakeUserRepository(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new DeleteRoleCommand(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
        {
            Role role = AdminRolesTestSupport.CreateRole(
                name: "SuperAdmin",
                isSystem: true);
            var roleWriteRepository = new AdminRolesTestSupport.FakeRoleWriteRepository();
            roleWriteRepository.RolesById[role.Id] = role;
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new DeleteRoleCommandHandler(
                roleWriteRepository: roleWriteRepository,
                userRepository: new AdminRolesTestSupport.FakeUserRepository(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new DeleteRoleCommand(role.Id),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.ReadOnly",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
            Assert.Empty(roleWriteRepository.DeletedRoles);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
