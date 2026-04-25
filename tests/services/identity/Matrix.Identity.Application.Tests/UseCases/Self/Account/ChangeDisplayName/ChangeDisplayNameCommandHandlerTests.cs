using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.ChangeDisplayName;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName.ChangeDisplayNameCommandHandler(
            userRepository,
            unitOfWork,
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_ThrowsAccountDeleted()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName.ChangeDisplayNameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand("The One"),
            CancellationToken.None));

        Assert.Equal("Identity.AccountDeleted", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDisplayNameInvalid_PropagatesDomainException()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName.ChangeDisplayNameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });
        string tooLongDisplayName = new('n', User.DisplayNameMaxLength + 1);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand(tooLongDisplayName),
            CancellationToken.None));

        Assert.Equal("Identity.User.DisplayName.InvalidLength", exception.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDisplayNameValid_NormalizesAndSaves()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName.ChangeDisplayNameCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        string? result = await handler.Handle(
            SelfServiceHandlerTestSupport.CreateChangeDisplayNameCommand("  Thomas Anderson  "),
            CancellationToken.None);

        Assert.Equal("Thomas Anderson", result);
        Assert.Equal("Thomas Anderson", user.DisplayName);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
