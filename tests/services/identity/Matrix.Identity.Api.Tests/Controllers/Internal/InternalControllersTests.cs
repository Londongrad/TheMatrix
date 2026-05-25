using Matrix.Identity.Api.Controllers.Internal;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Internal
{
    public sealed class InternalControllersTests
    {
        [Fact]
        public async Task GetPermissionsVersion_WhenUserExists_ReturnsVersion()
        {
            var userId = Guid.Parse("d04056da-be2a-486b-8d6d-09889ee76fae");
            var userRepository = new FakeUserRepository();
            var permissionsService = new FakeEffectivePermissionsService();
            userRepository.PermissionsVersions[userId] = 17;
            var controller = new InternalUsersController(
                userRepository: userRepository,
                effectivePermissionsService: permissionsService);

            ActionResult<PermissionsVersionResponse> actionResult = await controller.GetPermissionsVersion(
                userId: userId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PermissionsVersionResponse response = Assert.IsType<PermissionsVersionResponse>(ok.Value);
            Assert.Equal(
                expected: 17,
                actual: response.Version);
        }

        [Fact]
        public async Task GetPermissionsVersion_WhenUserIsMissing_ReturnsNotFound()
        {
            var controller = new InternalUsersController(
                userRepository: new FakeUserRepository(),
                effectivePermissionsService: new FakeEffectivePermissionsService());

            ActionResult<PermissionsVersionResponse> actionResult = await controller.GetPermissionsVersion(
                userId: Guid.Parse("73e4c2fe-5871-40d6-a2b2-7644892e2ef6"),
                cancellationToken: CancellationToken.None);

            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetAuthContext_SortsAndDeduplicatesPermissions()
        {
            var userId = Guid.Parse("d6407d6d-8815-457a-80c5-89d6d4ac503f");
            var permissionsService = new FakeEffectivePermissionsService
            {
                Result = CreateAuthorizationContext(
                    permissionsVersion: 9,
                    permissions:
                    [
                        "users.write",
                        "",
                        "users.read",
                        "users.write",
                        "users.delete"
                    ])
            };
            var controller = new InternalUsersController(
                userRepository: new FakeUserRepository(),
                effectivePermissionsService: permissionsService);

            ActionResult<UserAuthContextResponse> actionResult = await controller.GetAuthContext(
                userId: userId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            UserAuthContextResponse response = Assert.IsType<UserAuthContextResponse>(ok.Value);
            Assert.Equal(
                expected: userId,
                actual: permissionsService.LastRequestedUserId);
            Assert.Equal(
                expected: 9,
                actual: response.PermissionsVersion);
            Assert.Equal(
                expectedSpan:
                [
                    "users.delete",
                    "users.read",
                    "users.write"
                ],
                actualArray: response.EffectivePermissions);
        }

        [Fact]
        public async Task GetAuthContext_WhenPermissionsServiceThrowsInvalidOperation_ReturnsNotFound()
        {
            var permissionsService = new FakeEffectivePermissionsService
            {
                Exception = new InvalidOperationException("user missing")
            };
            var controller = new InternalUsersController(
                userRepository: new FakeUserRepository(),
                effectivePermissionsService: permissionsService);

            ActionResult<UserAuthContextResponse> actionResult = await controller.GetAuthContext(
                userId: Guid.Parse("1ca3b883-0bda-4c06-b883-9ad8b344af9b"),
                cancellationToken: CancellationToken.None);

            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetDefaultUserAccessVersion_ReturnsConfiguredVersion()
        {
            var repository = new FakeDefaultUserAccessPolicyRepository
            {
                Version = 42
            };
            var controller = new InternalAuthorizationController(repository);

            ActionResult<DefaultUserAccessVersionResponse> actionResult =
                await controller.GetDefaultUserAccessVersion(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            DefaultUserAccessVersionResponse response = Assert.IsType<DefaultUserAccessVersionResponse>(ok.Value);
            Assert.Equal(
                expected: 42,
                actual: response.Version);
        }
    }
}
