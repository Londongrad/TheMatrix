using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserRoles
{
    public sealed class GetUserRolesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRolesExist_ReturnsRolesWithoutExistenceCheck()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository();
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
            {
                GetUserRolesResult =
                [
                    new UserRoleResult
                    {
                        Id = Guid.NewGuid(),
                        Name = "Operators",
                        IsSystem = false,
                        CreatedAtUtc = AdminUsersTestSupport.UtcNow.AddDays(-4)
                    }
                ]
            };
            var handler = new GetUserRolesQueryHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository);

            IReadOnlyCollection<UserRoleResult> result = await handler.Handle(
                request: new GetUserRolesQuery(userId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: userId,
                actual: userRolesRepository.RequestedUserId);
            Assert.Null(userRepository.RequestedUserId);
            Assert.Single(result);
        }

        [Fact]
        public async Task Handle_WhenUserHasNoRolesAndDoesNotExist_ThrowsNotFound()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = false
            };
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
            var handler = new GetUserRolesQueryHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetUserRolesQuery(userId),
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
        public async Task Handle_WhenUserExistsAndHasNoRoles_ReturnsEmptyCollection()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
            var handler = new GetUserRolesQueryHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository);

            IReadOnlyCollection<UserRoleResult> result = await handler.Handle(
                request: new GetUserRolesQuery(userId),
                cancellationToken: CancellationToken.None);

            Assert.Empty(result);
            Assert.Equal(
                expected: userId,
                actual: userRepository.RequestedUserId);
        }
    }
}
