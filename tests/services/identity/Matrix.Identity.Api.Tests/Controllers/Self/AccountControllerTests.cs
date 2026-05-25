using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Controllers.Self;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount;
using Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Self
{
    public sealed class AccountControllerTests
    {
        [Fact]
        public async Task GetProfile_MapsProfileResult()
        {
            var userId = Guid.Parse("34cd6380-91d0-405b-b323-cb99c123afcc");
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            sender.Handle<GetMyProfileQuery, MyProfileResult>(_ => new MyProfileResult
            {
                UserId = userId,
                Email = "neo@matrix.local",
                PendingEmail = "thomas@matrix.local",
                Username = "neo",
                DisplayName = "The One",
                AvatarUrl = "/avatars/neo.png",
                IsEmailConfirmed = true,
                CreatedAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                EmailConfirmedAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                EffectivePermissions =
                [
                    "identity.me.read",
                    "identity.me.write"
                ],
                PermissionsVersion = 17
            });
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(path: "/api/account/profile"));

            ActionResult<UserProfileResponse> actionResult = await controller.GetProfile(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            UserProfileResponse response = Assert.IsType<UserProfileResponse>(ok.Value);

            Assert.Equal(
                expected: userId,
                actual: response.UserId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: response.Email);
            Assert.Equal(
                expected: "thomas@matrix.local",
                actual: response.PendingEmail);
            Assert.Equal(
                expected: "neo",
                actual: response.Username);
            Assert.Equal(
                expected: "The One",
                actual: response.DisplayName);
            Assert.Equal(
                expectedSpan:
                [
                    "identity.me.read",
                    "identity.me.write"
                ],
                actualArray: response.EffectivePermissions);
            Assert.Equal(
                expected: 17,
                actual: response.PermissionsVersion);
        }

        [Fact]
        public async Task GetSecurityActivity_MapsCursorPage()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            sender.Handle<GetMySecurityActivityQuery, CursorPagedResult<SecurityActivityItemResult>>(query =>
            {
                Assert.Equal(
                    expected: "cursor-1",
                    actual: query.Cursor);
                Assert.Equal(
                    expected: 25,
                    actual: query.PageSize);

                return new CursorPagedResult<SecurityActivityItemResult>(
                    items:
                    [
                        new SecurityActivityItemResult
                        {
                            EventId = Guid.Parse("9ff1a415-b9a8-45eb-bf28-72966bf8f9db"),
                            EventType = SecurityAuditEventType.Login,
                            IsSuccessful = true,
                            OccurredAtUtc = new DateTime(
                                year: 2048,
                                month: 6,
                                day: 1,
                                hour: 10,
                                minute: 0,
                                second: 0,
                                kind: DateTimeKind.Utc),
                            IpAddress = "203.0.113.55",
                            UserAgent = "Browser/2.0",
                            DeviceId = "device-2",
                            DeviceName = "Laptop",
                            Details = "ok"
                        }
                    ],
                    pageSize: 25,
                    nextCursor: "cursor-2");
            });
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(path: "/api/account/security-activity"));

            ActionResult<CursorPagedResult<SecurityActivityItemResponse>> actionResult =
                await controller.GetSecurityActivity(
                    cursor: "cursor-1",
                    pageSize: 25,
                    cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CursorPagedResult<SecurityActivityItemResponse> response =
                Assert.IsType<CursorPagedResult<SecurityActivityItemResponse>>(ok.Value);

            SecurityActivityItemResponse item = Assert.Single(response.Items);
            Assert.Equal(
                expected: "cursor-2",
                actual: response.NextCursor);
            Assert.Equal(
                expected: "Login",
                actual: item.EventType);
            Assert.Equal(
                expected: "203.0.113.55",
                actual: item.IpAddress);
        }

        [Fact]
        public async Task ChangeEmail_ForwardsTrustedGatewayMetadata()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            sender.Handle<RequestEmailChangeCommand, string>(_ => "smith@matrix.local");
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(
                    path: "/api/account/email",
                    remoteIp: "198.51.100.10",
                    forwardedClientIp: "203.0.113.90",
                    trustedGateway: true,
                    userAgent: "Mozilla/5.0"));

            ActionResult<ChangeEmailResponse> actionResult = await controller.ChangeEmail(
                request: new ChangeEmailRequest
                {
                    NewEmail = "smith@matrix.local",
                    CurrentPassword = "N3b!ch"
                },
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            ChangeEmailResponse response = Assert.IsType<ChangeEmailResponse>(ok.Value);
            RequestEmailChangeCommand command = Assert.IsType<RequestEmailChangeCommand>(sender.Requests.Single());

            Assert.Equal(
                expected: "smith@matrix.local",
                actual: command.NewEmail);
            Assert.Equal(
                expected: "N3b!ch",
                actual: command.CurrentPassword);
            Assert.Equal(
                expected: "203.0.113.90",
                actual: command.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0",
                actual: command.UserAgent);
            Assert.Equal(
                expected: "smith@matrix.local",
                actual: response.PendingEmail);
        }

        [Fact]
        public async Task ChangeAvatar_WhenAvatarIsMissing_ReturnsBadRequest()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(path: "/api/account/avatar"));

            ActionResult<ChangeAvatarResponse> actionResult = await controller.ChangeAvatar(
                avatar: null,
                cancellationToken: CancellationToken.None);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal(
                expected: "Avatar file is required.",
                actual: badRequest.Value);
            Assert.Empty(sender.Requests);
        }

        [Fact]
        public async Task ChangeAvatar_WhenAvatarIsProvided_SendsFileCommandAndMapsUrl()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            sender.Handle<ChangeAvatarFromFileCommand, string>(_ => "/avatars/neo-updated.png");
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(path: "/api/account/avatar"));
            IFormFile avatar = new FormFile(
                baseStream: new MemoryStream(
                [
                    1,
                    2,
                    3,
                    4
                ]),
                baseStreamOffset: 0,
                length: 4,
                name: "avatar",
                fileName: "neo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            ActionResult<ChangeAvatarResponse> actionResult = await controller.ChangeAvatar(
                avatar: avatar,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            ChangeAvatarResponse response = Assert.IsType<ChangeAvatarResponse>(ok.Value);
            ChangeAvatarFromFileCommand command = Assert.IsType<ChangeAvatarFromFileCommand>(sender.Requests.Single());

            Assert.Equal(
                expected: "neo.png",
                actual: command.FileName);
            Assert.Equal(
                expected: "image/png",
                actual: command.ContentType);
            Assert.Equal(
                expected: 4,
                actual: command.FileSize);
            Assert.Equal(
                expected: "/avatars/neo-updated.png",
                actual: response.AvatarUrl);
        }

        [Fact]
        public async Task GetAvatar_WhenStorageHasFile_ReturnsFileContent()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage
            {
                OpenReadResult = new AvatarFileReadResult(
                    Content: new MemoryStream(
                    [
                        8,
                        6,
                        7,
                        5,
                        3,
                        0,
                        9
                    ]),
                    ContentType: "image/png")
            };
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(path: "/avatars/neo.png"));

            IActionResult result = await controller.GetAvatar(
                fileName: "neo.png",
                cancellationToken: CancellationToken.None);

            FileContentResult file = Assert.IsType<FileContentResult>(result);
            Assert.Equal(
                expected: "/avatars/neo.png",
                actual: avatarStorage.LastOpenedPath);
            Assert.Equal(
                expected: "image/png",
                actual: file.ContentType);
            Assert.Equal(
                expected:
                [
                    8,
                    6,
                    7,
                    5,
                    3,
                    0,
                    9
                ],
                actual: file.FileContents);
        }

        [Fact]
        public async Task DeleteAccount_SendsUserAgentAndResolvedIp()
        {
            var sender = new FakeSender();
            var avatarStorage = new FakeAvatarStorage();
            sender.Handle<DeleteMyAccountCommand>(_ => { });
            AccountController controller = AttachHttpContext(
                controller: new AccountController(
                    sender: sender,
                    avatarStorage: avatarStorage),
                httpContext: CreateHttpContext(
                    path: "/api/account/delete",
                    remoteIp: "198.51.100.33",
                    userAgent: "DeleteAgent/1.0"));

            IActionResult result = await controller.DeleteAccount(
                request: new DeleteAccountRequest
                {
                    CurrentPassword = "Z1on!n"
                },
                cancellationToken: CancellationToken.None);

            DeleteMyAccountCommand command = Assert.IsType<DeleteMyAccountCommand>(sender.Requests.Single());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected: "Z1on!n",
                actual: command.CurrentPassword);
            Assert.Equal(
                expected: "198.51.100.33",
                actual: command.IpAddress);
            Assert.Equal(
                expected: "DeleteAgent/1.0",
                actual: command.UserAgent);
        }
    }
}
