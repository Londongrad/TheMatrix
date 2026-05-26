using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;
using static Matrix.SimulationSystems.Application.Tests.TestSupport.SimulationSystemsApplicationTestSupport;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public sealed class DeleteCitySystemsDataCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenDeletionIsNew_RemovesStateAndRecordsTombstone()
        {
            var conditionRepository = new FakeCityEnvironmentalConditionRepository
            {
                State = CreateState()
            };
            var deletionStateRepository = new FakeCitySystemsDeletionStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            DateTimeOffset deletedAtUtc = LaterUtc.AddMinutes(10);
            DateTimeOffset processedAtUtc = LaterUtc.AddMinutes(15);
            var handler = new DeleteCitySystemsDataCommandHandler(
                conditionRepository: conditionRepository,
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork,
                timeProvider: CreateTimeProvider(processedAtUtc));

            DeleteCitySystemsDataResult result = await handler.Handle(
                request: new DeleteCitySystemsDataCommand(
                    CityId: CityId,
                    DeletedAtUtc: deletedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(DeleteCitySystemsDataStatus.Applied, result.Status);
            Assert.Equal(1, conditionRepository.DeleteCallCount);
            Assert.Null(conditionRepository.State);
            Assert.Equal(deletedAtUtc, deletionStateRepository.DeletedAtUtc);
            Assert.Equal(processedAtUtc, deletionStateRepository.UpdatedAtUtc);
            Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        }

        [Theory]
        [InlineData(0, DeleteCitySystemsDataStatus.Duplicate)]
        [InlineData(-1, DeleteCitySystemsDataStatus.Stale)]
        public async Task Handle_WhenTombstoneAlreadyCoversDeletion_DoesNotMutate(
            int eventOffsetMinutes,
            DeleteCitySystemsDataStatus expectedStatus)
        {
            DateTimeOffset existingDeletedAtUtc = LaterUtc.AddMinutes(10);
            var conditionRepository = new FakeCityEnvironmentalConditionRepository
            {
                State = CreateState()
            };
            var deletionStateRepository = new FakeCitySystemsDeletionStateRepository
            {
                DeletedAtUtc = existingDeletedAtUtc
            };
            var unitOfWork = new FakeUnitOfWork();
            var handler = new DeleteCitySystemsDataCommandHandler(
                conditionRepository: conditionRepository,
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork,
                timeProvider: CreateTimeProvider());

            DeleteCitySystemsDataResult result = await handler.Handle(
                request: new DeleteCitySystemsDataCommand(
                    CityId: CityId,
                    DeletedAtUtc: existingDeletedAtUtc.AddMinutes(eventOffsetMinutes)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(0, conditionRepository.DeleteCallCount);
            Assert.Equal(0, deletionStateRepository.RecordCallCount);
            Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        }
    }
}
