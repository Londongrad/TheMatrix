using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangePassword;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangePassword.ChangePasswordCommandHandler(
            userRepository,
            passwordHasher,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangePasswordCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Empty(passwordHasher.VerifyCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_ThrowsUnauthorizedAndDoesNotPersist()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangePassword.ChangePasswordCommandHandler(
            userRepository,
            passwordHasher,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangePasswordCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCurrentPassword", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Single(passwordHasher.VerifyCalls);
        Assert.Empty(passwordHasher.HashedPasswords);
        Assert.Equal("stored-hash", user.PasswordHash);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordValid_UpdatesPasswordHashAndSaves()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangePassword.ChangePasswordCommandHandler(
            userRepository,
            passwordHasher,
            unitOfWork,
            currentUser);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangePasswordCommand(
                currentPassword: "CurrentPa$$w0rd",
                newPassword: "NewPa$$w0rd"),
            CancellationToken.None);

        var verify = Assert.Single(passwordHasher.VerifyCalls);
        Assert.Equal(user.Id, verify.UserId);
        Assert.Equal("stored-hash", verify.PasswordHash);
        Assert.Equal("CurrentPa$$w0rd", verify.ProvidedPassword);
        Assert.Equal(new[] { "NewPa$$w0rd" }, passwordHasher.HashedPasswords);
        Assert.Equal("hash::NewPa$$w0rd", user.PasswordHash);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
