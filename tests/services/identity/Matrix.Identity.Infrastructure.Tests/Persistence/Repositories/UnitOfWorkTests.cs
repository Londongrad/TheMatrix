using System.Data;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenNoAmbientTransaction_PersistsChangesAndProcessesSecurityState()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var processor = new FakeSecurityStateChangeProcessor();
        var unitOfWork = new UnitOfWork(database.DbContext, processor, new TestLogger<UnitOfWork>());
        User user = CreateUser();

        await unitOfWork.ExecuteInTransactionAsync(
            async _ =>
            {
                await database.DbContext.Users.AddAsync(user);
            },
            CancellationToken.None,
            IsolationLevel.Serializable);

        Assert.Equal(1, await database.DbContext.Users.CountAsync());
        Assert.Equal(1, processor.CallCount);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenAmbientTransactionExists_SkipsSecurityProcessor()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var processor = new FakeSecurityStateChangeProcessor();
        var unitOfWork = new UnitOfWork(database.DbContext, processor, new TestLogger<UnitOfWork>());

        await using var transaction = await database.DbContext.Database.BeginTransactionAsync();

        await unitOfWork.ExecuteInTransactionAsync(
            async _ =>
            {
                await database.DbContext.Users.AddAsync(CreateUser());
            },
            CancellationToken.None);
        await transaction.CommitAsync();

        Assert.Equal(1, await database.DbContext.Users.CountAsync());
        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenActionThrowsApplicationException_PreservesException()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var unitOfWork = new UnitOfWork(database.DbContext, new FakeSecurityStateChangeProcessor(), new TestLogger<UnitOfWork>());
        var expected = ApplicationErrorsFactory.UserNotFound(Guid.Parse("d765ad8e-c707-4d1a-9d24-38ddce8ef00e"));

        Matrix.BuildingBlocks.Application.Exceptions.MatrixApplicationException exception = await Assert.ThrowsAsync<Matrix.BuildingBlocks.Application.Exceptions.MatrixApplicationException>(
            () => unitOfWork.ExecuteInTransactionAsync(
                _ => throw expected,
                CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenUnexpectedExceptionOccurs_WrapsIntoInfrastructureException()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var unitOfWork = new UnitOfWork(database.DbContext, new FakeSecurityStateChangeProcessor(), new TestLogger<UnitOfWork>());

        MatrixInfrastructureException exception = await Assert.ThrowsAsync<MatrixInfrastructureException>(
            () => unitOfWork.ExecuteInTransactionAsync(
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        Assert.Equal("Infrastructure.UnitOfWorkFailed", exception.Code);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
}
