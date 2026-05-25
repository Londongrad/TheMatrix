using Matrix.Identity.Api.Controllers.Self;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken;
using Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser;
using Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Self
{
    public sealed class AuthControllerTests
    {
        [Fact]
        public async Task Login_MapsResponseAndForwardsTrustedGatewayIp()
        {
            var sender = new FakeSender();
            sender.Handle<LoginUserCommand, LoginUserResult>(_ => CreateLoginUserResult());
            AuthController controller = AttachHttpContext(
                controller: new AuthController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/auth/login",
                    remoteIp: "198.51.100.10",
                    forwardedClientIp: "203.0.113.77",
                    trustedGateway: true,
                    userAgent: "Mozilla/5.0"));

            ActionResult<LoginResponse> actionResult = await controller.Login(
                request: new LoginRequest
                {
                    Login = "neo",
                    Password = "Tr1n1ty!",
                    DeviceId = "device-42",
                    DeviceName = "Desktop",
                    RememberMe = true
                },
                cancellationToken: default(CancellationToken));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            LoginResponse response = Assert.IsType<LoginResponse>(ok.Value);
            LoginUserCommand command = Assert.IsType<LoginUserCommand>(sender.Requests.Single());

            Assert.Equal(
                expected: "neo",
                actual: command.Login);
            Assert.Equal(
                expected: "device-42",
                actual: command.DeviceId);
            Assert.Equal(
                expected: "Desktop",
                actual: command.DeviceName);
            Assert.Equal(
                expected: "Mozilla/5.0",
                actual: command.UserAgent);
            Assert.Equal(
                expected: "203.0.113.77",
                actual: command.IpAddress);
            Assert.True(command.RememberMe);
            Assert.Equal(
                expected: "access-token",
                actual: response.AccessToken);
            Assert.Equal(
                expected: "refresh-token",
                actual: response.RefreshToken);
            Assert.True(response.IsPersistent);
        }

        [Fact]
        public async Task Refresh_UsesNormalizedRemoteIpAndMapsResponse()
        {
            var sender = new FakeSender();
            sender.Handle<RefreshTokenCommand, LoginUserResult>(_ => CreateLoginUserResult(
                accessToken: "access-2",
                refreshToken: "refresh-2",
                isPersistent: false));
            AuthController controller = AttachHttpContext(
                controller: new AuthController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/auth/refresh",
                    remoteIp: "::ffff:198.51.100.20",
                    trustedGateway: false,
                    userAgent: "RefreshAgent/2.0"));

            ActionResult<LoginResponse> actionResult = await controller.Refresh(
                request: new RefreshRequest
                {
                    RefreshToken = "rt-1",
                    DeviceId = "device-55"
                },
                cancellationToken: default(CancellationToken));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            LoginResponse response = Assert.IsType<LoginResponse>(ok.Value);
            RefreshTokenCommand command = Assert.IsType<RefreshTokenCommand>(sender.Requests.Single());

            Assert.Equal(
                expected: "rt-1",
                actual: command.RefreshToken);
            Assert.Equal(
                expected: "device-55",
                actual: command.DeviceId);
            Assert.Equal(
                expected: "RefreshAgent/2.0",
                actual: command.UserAgent);
            Assert.Equal(
                expected: "198.51.100.20",
                actual: command.IpAddress);
            Assert.Equal(
                expected: "access-2",
                actual: response.AccessToken);
            Assert.False(response.IsPersistent);
        }

        [Fact]
        public async Task Logout_ReturnsNoContentAndSendsCommand()
        {
            var sender = new FakeSender();
            sender.Handle<RevokeRefreshTokenCommand>(_ => { });
            AuthController controller = AttachHttpContext(
                controller: new AuthController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/auth/logout",
                    remoteIp: "198.51.100.30",
                    userAgent: "LogoutAgent/1.0"));

            IActionResult result = await controller.Logout(
                request: new LogoutRequest
                {
                    RefreshToken = "logout-token"
                },
                cancellationToken: default(CancellationToken));

            RevokeRefreshTokenCommand command = Assert.IsType<RevokeRefreshTokenCommand>(sender.Requests.Single());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected: "logout-token",
                actual: command.RefreshToken);
            Assert.Equal(
                expected: "198.51.100.30",
                actual: command.IpAddress);
            Assert.Equal(
                expected: "LogoutAgent/1.0",
                actual: command.UserAgent);
        }

        [Fact]
        public async Task Register_MapsRegisterResult()
        {
            var userId = Guid.Parse("57a941e2-6b08-43a8-81c1-0fe67311755d");
            var sender = new FakeSender();
            sender.Handle<RegisterUserCommand, RegisterUserResult>(_ => new RegisterUserResult
            {
                UserId = userId,
                Email = "neo@matrix.local",
                Username = "neo"
            });
            AuthController controller = AttachHttpContext(
                controller: new AuthController(sender),
                httpContext: CreateHttpContext(path: "/api/auth/register"));

            ActionResult<RegisterResponse> actionResult = await controller.Register(
                request: new RegisterRequest
                {
                    Email = "neo@matrix.local",
                    Username = "neo",
                    Password = "Sup3r$ecret"
                },
                cancellationToken: default(CancellationToken));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            RegisterResponse response = Assert.IsType<RegisterResponse>(ok.Value);
            RegisterUserCommand command = Assert.IsType<RegisterUserCommand>(sender.Requests.Single());

            Assert.Equal(
                expected: "neo@matrix.local",
                actual: command.Email);
            Assert.Equal(
                expected: "neo",
                actual: command.Username);
            Assert.Equal(
                expected: userId,
                actual: response.UserId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: response.Email);
        }
    }
}
