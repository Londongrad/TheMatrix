using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeAvatarFromFile
{
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
            var handler = new ChangeAvatarFromFileCommandHandler(
                userRepository: userRepository,
                avatarStorage: avatarStorage,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Empty(avatarStorage.DeletedPaths);
            Assert.Empty(avatarStorage.SaveRequests);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenOldAvatarExists_DeletesOldAvatarSavesNewAvatarAndUpdatesUser()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            user.ChangeAvatar("avatars/old-avatar.png");
            var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage
            {
                SaveResult = "avatars/new-avatar.png"
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            using var stream = new MemoryStream(
                new byte[]
                {
                    1,
                    2,
                    3,
                    4,
                    5
                });
            var handler = new ChangeAvatarFromFileCommandHandler(
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

            string result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(
                    fileStream: stream,
                    fileName: "avatar.png",
                    contentType: "image/png"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "avatars/new-avatar.png",
                actual: result);
            Assert.Equal(
                expected: "avatars/new-avatar.png",
                actual: user.AvatarUrl);
            Assert.Equal(
                expected: new[]
                {
                    "avatars/old-avatar.png"
                },
                actual: avatarStorage.DeletedPaths);
            (Stream Content, string FileName, string ContentType) saveRequest =
                Assert.Single(avatarStorage.SaveRequests);
            Assert.Same(
                expected: stream,
                actual: saveRequest.Content);
            Assert.Equal(
                expected: "avatar.png",
                actual: saveRequest.FileName);
            Assert.Equal(
                expected: "image/png",
                actual: saveRequest.ContentType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenNoOldAvatar_SavesNewAvatarWithoutDelete()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var avatarStorage = new SelfServiceHandlerTestSupport.FakeAvatarStorage
            {
                SaveResult = "avatars/neo.png"
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            using var stream = new MemoryStream(
                new byte[]
                {
                    9,
                    8,
                    7
                });
            var handler = new ChangeAvatarFromFileCommandHandler(
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

            string result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeAvatarFromFileCommand(
                    fileStream: stream,
                    fileName: "neo.webp",
                    contentType: "image/webp"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "avatars/neo.png",
                actual: result);
            Assert.Equal(
                expected: "avatars/neo.png",
                actual: user.AvatarUrl);
            Assert.Empty(avatarStorage.DeletedPaths);
            (Stream Content, string FileName, string ContentType) saveRequest =
                Assert.Single(avatarStorage.SaveRequests);
            Assert.Same(
                expected: stream,
                actual: saveRequest.Content);
            Assert.Equal(
                expected: "neo.webp",
                actual: saveRequest.FileName);
            Assert.Equal(
                expected: "image/webp",
                actual: saveRequest.ContentType);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
