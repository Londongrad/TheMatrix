using System.Data;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class UnitOfWorkTests
    {
        [Fact]
        public async Task ExecuteInTransactionAsync_WhenNoAmbientTransaction_PersistsChangesAndProcessesSecurityState()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var processor = new FakeSecurityStateChangeProcessor();
            var unitOfWork = new UnitOfWork(
                dbContext: database.DbContext,
                securityStateChangeProcessor: processor,
                logger: new TestLogger<UnitOfWork>());
            User user = CreateUser();

            await unitOfWork.ExecuteInTransactionAsync(
                action: async _ => { await database.DbContext.Users.AddAsync(user); },
                cancellationToken: CancellationToken.None,
                isolationLevel: IsolationLevel.Serializable);

            Assert.Equal(
                expected: 1,
                actual: await database.DbContext.Users.CountAsync());
            Assert.Equal(
                expected: 1,
                actual: processor.CallCount);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhenAmbientTransactionExists_SkipsSecurityProcessor()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var processor = new FakeSecurityStateChangeProcessor();
            var unitOfWork = new UnitOfWork(
                dbContext: database.DbContext,
                securityStateChangeProcessor: processor,
                logger: new TestLogger<UnitOfWork>());

            await using IDbContextTransaction transaction = await database.DbContext.Database.BeginTransactionAsync();

            await unitOfWork.ExecuteInTransactionAsync(
                action: async _ => { await database.DbContext.Users.AddAsync(CreateUser()); },
                cancellationToken: CancellationToken.None);
            await transaction.CommitAsync();

            Assert.Equal(
                expected: 1,
                actual: await database.DbContext.Users.CountAsync());
            Assert.Equal(
                expected: 0,
                actual: processor.CallCount);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhenActionThrowsApplicationException_PreservesException()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var unitOfWork = new UnitOfWork(
                dbContext: database.DbContext,
                securityStateChangeProcessor: new FakeSecurityStateChangeProcessor(),
                logger: new TestLogger<UnitOfWork>());
            MatrixApplicationException expected =
                ApplicationErrorsFactory.UserNotFound(Guid.Parse("d765ad8e-c707-4d1a-9d24-38ddce8ef00e"));

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => unitOfWork.ExecuteInTransactionAsync(
                    action: _ => throw expected,
                    cancellationToken: CancellationToken.None));

            Assert.Same(
                expected: expected,
                actual: exception);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_WhenUnexpectedExceptionOccurs_WrapsIntoInfrastructureException()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var unitOfWork = new UnitOfWork(
                dbContext: database.DbContext,
                securityStateChangeProcessor: new FakeSecurityStateChangeProcessor(),
                logger: new TestLogger<UnitOfWork>());

            MatrixInfrastructureException exception = await Assert.ThrowsAsync<MatrixInfrastructureException>(()
                => unitOfWork.ExecuteInTransactionAsync(
                    action: _ => throw new InvalidOperationException("boom"),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Infrastructure.UnitOfWorkFailed",
                actual: exception.Code);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
    }
}
