using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeDisplayName
{
    public sealed class ChangeDisplayNameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("90000000-0000-0000-0000-000000000001")
            };
            var handler = new ChangeDisplayNameCommandHandler(
                userRepository: userRepository,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: currentUser.UserId,
                actual: userRepository.RequestedUserId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenUserDeleted_ThrowsAccountDeleted()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeDisplayNameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand("The One"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.AccountDeleted",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDisplayNameInvalid_PropagatesDomainException()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeDisplayNameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });
            string tooLongDisplayName = new(
                c: 'n',
                count: User.DisplayNameMaxLength + 1);

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand(tooLongDisplayName),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.DisplayName.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDisplayNameValid_NormalizesAndSaves()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var handler = new ChangeDisplayNameCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                unitOfWork: unitOfWork,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            string? result = await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand("  Thomas Anderson  "),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Thomas Anderson",
                actual: result);
            Assert.Equal(
                expected: "Thomas Anderson",
                actual: user.DisplayName);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
