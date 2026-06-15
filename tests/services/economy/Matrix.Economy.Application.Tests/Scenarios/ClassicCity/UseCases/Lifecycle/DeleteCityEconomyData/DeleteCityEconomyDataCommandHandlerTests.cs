using System.Data;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData
{
    public sealed class DeleteCityEconomyDataCommandHandlerTests
    {
        private static readonly Guid CityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTimeOffset DeletedAtUtc = new(
            year: 2048,
            month: 6,
            day: 2,
            hour: 9,
            minute: 30,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task Handle_FirstDeletion_RemovesDataAndRecordsTombstone()
        {
            var repository = new FakeCityEconomyDeletionRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(DeletedAtUtc.AddMinutes(2));
            var handler = new DeleteCityEconomyDataCommandHandler(
                repository,
                unitOfWork,
                timeProvider);

            DeleteCityEconomyDataResult result = await handler.Handle(
                request: new DeleteCityEconomyDataCommand(CityId, DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(DeleteCityEconomyDataStatus.Applied, result.Status);
            Assert.Equal(1, repository.DeleteCityDataCallCount);
            Assert.Equal(1, repository.RecordCallCount);
            Assert.Equal(DeletedAtUtc, repository.RecordedDeletedAtUtc);
            Assert.Equal(timeProvider.UtcNow, repository.RecordedUpdatedAtUtc);
            Assert.Equal(1, unitOfWork.SaveChangesCallCount);
            Assert.Equal(1, unitOfWork.TransactionCallCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Theory]
        [InlineData(0, DeleteCityEconomyDataStatus.Duplicate)]
        [InlineData(1, DeleteCityEconomyDataStatus.Stale)]
        public async Task Handle_ReplayedOrOlderDeletion_DoesNotMutateData(
            int existingDeletionOffsetMinutes,
            DeleteCityEconomyDataStatus expectedStatus)
        {
            var repository = new FakeCityEconomyDeletionRepository
            {
                DeletedAtUtc = DeletedAtUtc.AddMinutes(existingDeletionOffsetMinutes)
            };
            var unitOfWork = new FakeEconomyUnitOfWork();
            var handler = new DeleteCityEconomyDataCommandHandler(
                repository,
                unitOfWork,
                new FrozenTimeProvider(DeletedAtUtc.AddMinutes(2)));

            DeleteCityEconomyDataResult result = await handler.Handle(
                request: new DeleteCityEconomyDataCommand(CityId, DeletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(0, repository.DeleteCityDataCallCount);
            Assert.Equal(0, repository.RecordCallCount);
            Assert.Equal(0, unitOfWork.SaveChangesCallCount);
            Assert.Equal(1, unitOfWork.TransactionCallCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }
    }
}
