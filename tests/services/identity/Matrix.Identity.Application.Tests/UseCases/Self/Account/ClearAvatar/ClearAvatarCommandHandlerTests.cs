using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ClearAvatar;

public sealed class ClearAvatarCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("91000000-0000-0000-0000-000000000001")
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar.ClearAvatarCommandHandler(
            userRepository,
            avatarStorage,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Empty(avatarStorage.DeletedPaths);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenAvatarExists_DeletesOldAvatarClearsPropertyAndSaves()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        user.ChangeAvatar("avatars/old-avatar.png");
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar.ClearAvatarCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            avatarStorage,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string? result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(user.AvatarUrl);
        Assert.Equal(new[] { "avatars/old-avatar.png" }, avatarStorage.DeletedPaths);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenAvatarMissing_DoesNotDeleteButStillSaves()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar.ClearAvatarCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            avatarStorage,
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string? result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(user.AvatarUrl);
        Assert.Empty(avatarStorage.DeletedPaths);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
