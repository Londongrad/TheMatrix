using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserPermissions
{
    public sealed class GetUserPermissionsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenOverridesExist_ReturnsOverridesWithoutExistenceCheck()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository();
            var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
            {
                GetUserPermissionsResult =
                [
                    new UserPermissionOverrideResult
                    {
                        PermissionKey = "users.read",
                        Effect = PermissionEffect.Allow
                    }
                ]
            };
            var handler = new GetUserPermissionsQueryHandler(
                userRepository: userRepository,
                permissionsRepository: permissionsRepository);

            IReadOnlyCollection<UserPermissionOverrideResult> result = await handler.Handle(
                request: new GetUserPermissionsQuery(userId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: userId,
                actual: permissionsRepository.RequestedUserId);
            Assert.Null(userRepository.RequestedUserId);
            Assert.Single(result);
        }

        [Fact]
        public async Task Handle_WhenOverridesEmptyAndUserDoesNotExist_ThrowsNotFound()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = false
            };
            var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository();
            var handler = new GetUserPermissionsQueryHandler(
                userRepository: userRepository,
                permissionsRepository: permissionsRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetUserPermissionsQuery(userId),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: userId,
                actual: userRepository.RequestedUserId);
        }

        [Fact]
        public async Task Handle_WhenUserExistsAndOverridesEmpty_ReturnsEmptyCollection()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository();
            var handler = new GetUserPermissionsQueryHandler(
                userRepository: userRepository,
                permissionsRepository: permissionsRepository);

            IReadOnlyCollection<UserPermissionOverrideResult> result = await handler.Handle(
                request: new GetUserPermissionsQuery(userId),
                cancellationToken: CancellationToken.None);

            Assert.Empty(result);
            Assert.Equal(
                expected: userId,
                actual: userRepository.RequestedUserId);
        }
    }
}
