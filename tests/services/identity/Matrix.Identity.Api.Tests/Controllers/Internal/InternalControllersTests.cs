using Matrix.Identity.Api.Controllers.Internal;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Internal;

public sealed class InternalControllersTests
{
    [Fact]
    public async Task GetPermissionsVersion_WhenUserExists_ReturnsVersion()
    {
        Guid userId = Guid.Parse("d04056da-be2a-486b-8d6d-09889ee76fae");
        var userRepository = new FakeUserRepository();
        var permissionsService = new FakeEffectivePermissionsService();
        userRepository.PermissionsVersions[userId] = 17;
        var controller = new InternalUsersController(userRepository, permissionsService);

        ActionResult<PermissionsVersionResponse> actionResult = await controller.GetPermissionsVersion(userId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        PermissionsVersionResponse response = Assert.IsType<PermissionsVersionResponse>(ok.Value);
        Assert.Equal(17, response.Version);
    }

    [Fact]
    public async Task GetPermissionsVersion_WhenUserIsMissing_ReturnsNotFound()
    {
        var controller = new InternalUsersController(new FakeUserRepository(), new FakeEffectivePermissionsService());

        ActionResult<PermissionsVersionResponse> actionResult = await controller.GetPermissionsVersion(
            Guid.Parse("73e4c2fe-5871-40d6-a2b2-7644892e2ef6"),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetAuthContext_SortsAndDeduplicatesPermissions()
    {
        Guid userId = Guid.Parse("d6407d6d-8815-457a-80c5-89d6d4ac503f");
        var permissionsService = new FakeEffectivePermissionsService
        {
            Result = CreateAuthorizationContext(
                permissionsVersion: 9,
                permissions: ["users.write", "", "users.read", "users.write", "users.delete"])
        };
        var controller = new InternalUsersController(new FakeUserRepository(), permissionsService);

        ActionResult<UserAuthContextResponse> actionResult = await controller.GetAuthContext(userId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        UserAuthContextResponse response = Assert.IsType<UserAuthContextResponse>(ok.Value);
        Assert.Equal(userId, permissionsService.LastRequestedUserId);
        Assert.Equal(9, response.PermissionsVersion);
        Assert.Equal(["users.delete", "users.read", "users.write"], response.EffectivePermissions);
    }

    [Fact]
    public async Task GetAuthContext_WhenPermissionsServiceThrowsInvalidOperation_ReturnsNotFound()
    {
        var permissionsService = new FakeEffectivePermissionsService
        {
            Exception = new InvalidOperationException("user missing")
        };
        var controller = new InternalUsersController(new FakeUserRepository(), permissionsService);

        ActionResult<UserAuthContextResponse> actionResult = await controller.GetAuthContext(
            Guid.Parse("1ca3b883-0bda-4c06-b883-9ad8b344af9b"),
            CancellationToken.None);

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

        ActionResult<DefaultUserAccessVersionResponse> actionResult = await controller.GetDefaultUserAccessVersion(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        DefaultUserAccessVersionResponse response = Assert.IsType<DefaultUserAccessVersionResponse>(ok.Value);
        Assert.Equal(42, response.Version);
    }
}
