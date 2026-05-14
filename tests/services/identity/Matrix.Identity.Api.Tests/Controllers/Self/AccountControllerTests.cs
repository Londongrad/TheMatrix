using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Controllers.Self;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername;
using Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar;
using Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount;
using Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange;
using Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Self;

public sealed class AccountControllerTests
{
    [Fact]
    public async Task GetProfile_MapsProfileResult()
    {
        Guid userId = Guid.Parse("34cd6380-91d0-405b-b323-cb99c123afcc");
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
            CreatedAtUtc = new DateTime(2048, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            EmailConfirmedAtUtc = new DateTime(2048, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            EffectivePermissions = ["identity.me.read", "identity.me.write"],
            PermissionsVersion = 17
        });
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
            httpContext: CreateHttpContext(path: "/api/account/profile"));

        ActionResult<UserProfileResponse> actionResult = await controller.GetProfile(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        UserProfileResponse response = Assert.IsType<UserProfileResponse>(ok.Value);

        Assert.Equal(userId, response.UserId);
        Assert.Equal("neo@matrix.local", response.Email);
        Assert.Equal("thomas@matrix.local", response.PendingEmail);
        Assert.Equal("neo", response.Username);
        Assert.Equal("The One", response.DisplayName);
        Assert.Equal(["identity.me.read", "identity.me.write"], response.EffectivePermissions);
        Assert.Equal(17, response.PermissionsVersion);
    }

    [Fact]
    public async Task GetSecurityActivity_MapsCursorPage()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage();
        sender.Handle<GetMySecurityActivityQuery, CursorPagedResult<SecurityActivityItemResult>>(query =>
        {
            Assert.Equal("cursor-1", query.Cursor);
            Assert.Equal(25, query.PageSize);

            return new CursorPagedResult<SecurityActivityItemResult>(
                items:
                [
                    new SecurityActivityItemResult
                    {
                        EventId = Guid.Parse("9ff1a415-b9a8-45eb-bf28-72966bf8f9db"),
                        EventType = Matrix.Identity.Application.Abstractions.Services.Security.SecurityAuditEventType.Login,
                        IsSuccessful = true,
                        OccurredAtUtc = new DateTime(2048, 6, 1, 10, 0, 0, DateTimeKind.Utc),
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
            controller: new AccountController(sender, avatarStorage),
            httpContext: CreateHttpContext(path: "/api/account/security-activity"));

        ActionResult<CursorPagedResult<SecurityActivityItemResponse>> actionResult = await controller.GetSecurityActivity(
            cursor: "cursor-1",
            pageSize: 25,
            cancellationToken: CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CursorPagedResult<SecurityActivityItemResponse> response =
            Assert.IsType<CursorPagedResult<SecurityActivityItemResponse>>(ok.Value);

        SecurityActivityItemResponse item = Assert.Single(response.Items);
        Assert.Equal("cursor-2", response.NextCursor);
        Assert.Equal("Login", item.EventType);
        Assert.Equal("203.0.113.55", item.IpAddress);
    }

    [Fact]
    public async Task ChangeEmail_ForwardsTrustedGatewayMetadata()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage();
        sender.Handle<RequestEmailChangeCommand, string>(_ => "smith@matrix.local");
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
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

        Assert.Equal("smith@matrix.local", command.NewEmail);
        Assert.Equal("N3b!ch", command.CurrentPassword);
        Assert.Equal("203.0.113.90", command.IpAddress);
        Assert.Equal("Mozilla/5.0", command.UserAgent);
        Assert.Equal("smith@matrix.local", response.PendingEmail);
    }

    [Fact]
    public async Task ChangeAvatar_WhenAvatarIsMissing_ReturnsBadRequest()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage();
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
            httpContext: CreateHttpContext(path: "/api/account/avatar"));

        ActionResult<ChangeAvatarResponse> actionResult = await controller.ChangeAvatar(
            avatar: null,
            cancellationToken: CancellationToken.None);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal("Avatar file is required.", badRequest.Value);
        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task ChangeAvatar_WhenAvatarIsProvided_SendsFileCommandAndMapsUrl()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage();
        sender.Handle<ChangeAvatarFromFileCommand, string>(_ => "/avatars/neo-updated.png");
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
            httpContext: CreateHttpContext(path: "/api/account/avatar"));
        IFormFile avatar = new FormFile(
            baseStream: new MemoryStream([1, 2, 3, 4]),
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

        Assert.Equal("neo.png", command.FileName);
        Assert.Equal("image/png", command.ContentType);
        Assert.Equal(4, command.FileSize);
        Assert.Equal("/avatars/neo-updated.png", response.AvatarUrl);
    }

    [Fact]
    public async Task GetAvatar_WhenStorageHasFile_ReturnsFileContent()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage
        {
            OpenReadResult = new AvatarFileReadResult(
                Content: new MemoryStream([8, 6, 7, 5, 3, 0, 9]),
                ContentType: "image/png")
        };
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
            httpContext: CreateHttpContext(path: "/avatars/neo.png"));

        IActionResult result = await controller.GetAvatar(
            fileName: "neo.png",
            cancellationToken: CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("/avatars/neo.png", avatarStorage.LastOpenedPath);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal([8, 6, 7, 5, 3, 0, 9], file.FileContents);
    }

    [Fact]
    public async Task DeleteAccount_SendsUserAgentAndResolvedIp()
    {
        var sender = new FakeSender();
        var avatarStorage = new FakeAvatarStorage();
        sender.Handle<DeleteMyAccountCommand>(_ => { });
        AccountController controller = AttachHttpContext(
            controller: new AccountController(sender, avatarStorage),
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
        Assert.Equal("Z1on!n", command.CurrentPassword);
        Assert.Equal("198.51.100.33", command.IpAddress);
        Assert.Equal("DeleteAgent/1.0", command.UserAgent);
    }
}
