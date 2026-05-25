using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ClearAvatar
{
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
            var handler = new ClearAvatarCommandHandler(
                userRepository: userRepository,
                avatarStorage: avatarStorage,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Empty(avatarStorage.DeletedPaths);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenAvatarExists_DeletesOldAvatarClearsPropertyAndSaves()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.ChangeAvatar("avatars/old-avatar.png");
            var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ClearAvatarCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                avatarStorage: avatarStorage,
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            string? result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Null(user.AvatarUrl);
            Assert.Equal(
                expected: new[]
                {
                    "avatars/old-avatar.png"
                },
                actual: avatarStorage.DeletedPaths);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenAvatarMissing_DoesNotDeleteButStillSaves()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ClearAvatarCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                avatarStorage: avatarStorage,
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            string? result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateClearAvatarCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Null(user.AvatarUrl);
            Assert.Empty(avatarStorage.DeletedPaths);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
