using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeAvatarFromFile;

public sealed class ChangeAvatarFromFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("91000000-0000-0000-0000-000000000002")
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile.ChangeAvatarFromFileCommandHandler(
            userRepository,
            avatarStorage,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Empty(avatarStorage.DeletedPaths);
        Assert.Empty(avatarStorage.SaveRequests);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenOldAvatarExists_DeletesOldAvatarSavesNewAvatarAndUpdatesUser()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.ChangeAvatar("avatars/old-avatar.png");
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage
        {
            SaveResult = "avatars/new-avatar.png"
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile.ChangeAvatarFromFileCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            avatarStorage,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(
                fileStream: stream,
                fileName: "avatar.png",
                contentType: "image/png"),
            CancellationToken.None);

        Assert.Equal("avatars/new-avatar.png", result);
        Assert.Equal("avatars/new-avatar.png", user.AvatarUrl);
        Assert.Equal(new[] { "avatars/old-avatar.png" }, avatarStorage.DeletedPaths);
        var saveRequest = Assert.Single(avatarStorage.SaveRequests);
        Assert.Same(stream, saveRequest.Content);
        Assert.Equal("avatar.png", saveRequest.FileName);
        Assert.Equal("image/png", saveRequest.ContentType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenNoOldAvatar_SavesNewAvatarWithoutDelete()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage
        {
            SaveResult = "avatars/neo.png"
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile.ChangeAvatarFromFileCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            avatarStorage,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(
                fileStream: stream,
                fileName: "neo.webp",
                contentType: "image/webp"),
            CancellationToken.None);

        Assert.Equal("avatars/neo.png", result);
        Assert.Equal("avatars/neo.png", user.AvatarUrl);
        Assert.Empty(avatarStorage.DeletedPaths);
        var saveRequest = Assert.Single(avatarStorage.SaveRequests);
        Assert.Same(stream, saveRequest.Content);
        Assert.Equal("neo.webp", saveRequest.FileName);
        Assert.Equal("image/webp", saveRequest.ContentType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
