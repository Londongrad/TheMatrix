using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources;
using Matrix.Resources.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public sealed class DeleteCityResourcesCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenDeletionIsNew_RemovesStateAndRecordsTombstone()
        {
            var stockpileRepository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            var deletionStateRepository = new FakeCityResourceDeletionStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            DateTimeOffset deletedAtUtc = LaterUtc.AddMinutes(10);
            DateTimeOffset processedAtUtc = LaterUtc.AddMinutes(15);
            var handler = new DeleteCityResourcesCommandHandler(
                stockpileRepository: stockpileRepository,
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork,
                timeProvider: CreateTimeProvider(processedAtUtc));

            DeleteCityResourcesResult result = await handler.Handle(
                request: new DeleteCityResourcesCommand(
                    CityId: CityId,
                    DeletedAtUtc: deletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(DeleteCityResourcesStatus.Applied, result.Status);
            Assert.Equal(1, stockpileRepository.DeleteCallCount);
            Assert.Null(stockpileRepository.State);
            Assert.Equal(deletedAtUtc, deletionStateRepository.DeletedAtUtc);
            Assert.Equal(processedAtUtc, deletionStateRepository.UpdatedAtUtc);
            Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        }

        [Theory]
        [InlineData(0, DeleteCityResourcesStatus.Duplicate)]
        [InlineData(-1, DeleteCityResourcesStatus.Stale)]
        public async Task Handle_WhenTombstoneAlreadyCoversDeletion_DoesNotMutate(
            int eventOffsetMinutes,
            DeleteCityResourcesStatus expectedStatus)
        {
            DateTimeOffset existingDeletedAtUtc = LaterUtc.AddMinutes(10);
            var stockpileRepository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            var deletionStateRepository = new FakeCityResourceDeletionStateRepository
            {
                DeletedAtUtc = existingDeletedAtUtc
            };
            var unitOfWork = new FakeUnitOfWork();
            var handler = new DeleteCityResourcesCommandHandler(
                stockpileRepository: stockpileRepository,
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork,
                timeProvider: CreateTimeProvider());

            DeleteCityResourcesResult result = await handler.Handle(
                request: new DeleteCityResourcesCommand(
                    CityId: CityId,
                    DeletedAtUtc: existingDeletedAtUtc.AddMinutes(eventOffsetMinutes)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(0, stockpileRepository.DeleteCallCount);
            Assert.Equal(0, deletionStateRepository.RecordCallCount);
            Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        }
    }
}
