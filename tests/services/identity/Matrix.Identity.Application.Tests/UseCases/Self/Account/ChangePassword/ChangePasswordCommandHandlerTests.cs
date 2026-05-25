using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Self.Account.ChangePassword;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangePassword
{
    public sealed class ChangePasswordCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("30000000-0000-0000-0000-000000000001")
            };
            var handler = new ChangePasswordCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangePasswordCommand(),
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
            Assert.Empty(passwordHasher.VerifyCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCurrentPasswordInvalid_ThrowsUnauthorizedAndDoesNotPersist()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
            {
                VerifyOutcome = PasswordVerificationOutcome.Failed
            };
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id
            };
            var handler = new ChangePasswordCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: SelfServiceHandlerTestSupport.CreateChangePasswordCommand(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.InvalidCurrentPassword",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Unauthorized,
                actual: exception.ErrorType);
            Assert.Single(passwordHasher.VerifyCalls);
            Assert.Empty(passwordHasher.HashedPasswords);
            Assert.Equal(
                expected: "stored-hash",
                actual: user.PasswordHash);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCurrentPasswordValid_UpdatesPasswordHashAndSaves()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            };
            var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = user.Id
            };
            var handler = new ChangePasswordCommandHandler(
                userRepository: userRepository,
                passwordHasher: passwordHasher,
                unitOfWork: unitOfWork,
                currentUser: currentUser);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateChangePasswordCommand(
                    currentPassword: "CurrentPa$$w0rd",
                    newPassword: "NewPa$$w0rd"),
                cancellationToken: CancellationToken.None);

            (Guid UserId, string PasswordHash, string ProvidedPassword) verify =
                Assert.Single(passwordHasher.VerifyCalls);
            Assert.Equal(
                expected: user.Id,
                actual: verify.UserId);
            Assert.Equal(
                expected: "stored-hash",
                actual: verify.PasswordHash);
            Assert.Equal(
                expected: "CurrentPa$$w0rd",
                actual: verify.ProvidedPassword);
            Assert.Equal(
                expected: new[]
                {
                    "NewPa$$w0rd"
                },
                actual: passwordHasher.HashedPasswords);
            Assert.Equal(
                expected: "hash::NewPa$$w0rd",
                actual: user.PasswordHash);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
