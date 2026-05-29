using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var instanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository();
            var handler = new ArchiveCityCommandHandler(
                simulationInstanceRepository: instanceRepository,
                cityRepository: cityRepository,
                simulationClockMutationExecutor: new SimulationTestSupport.FakeSimulationClockMutationExecutor(),
                outboxWriter: new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

            bool result = await handler.Handle(
                request: new ArchiveCityCommand(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
            Assert.Null(instanceRepository.RequestedSimulationId);
        }

        [Fact]
        public async Task Handle_WhenCityIsActive_PausesClockArchivesCityPublishesEventAndSaves()
        {
            var archivedAtUtc = DateTimeOffset.Parse("2048-06-01T10:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity();
            city.ClearDomainEvents();
            SimulationClock clock = SimulationTestSupport.CreateClock(
                simulationId: city.Id.Value,
                state: ClockState.Running);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            SimulationInstance instance = SimulationTestSupport.CreateInstance(city);
            instance.ClearDomainEvents();
            var instanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository
            {
                InstanceById = instance
            };
            var mutationExecutor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
            {
                Clock = clock,
                Result = true
            };
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var timeProvider = new ApplicationTestSupport.FixedTimeProvider(archivedAtUtc);
            var handler = new ArchiveCityCommandHandler(
                simulationInstanceRepository: instanceRepository,
                cityRepository: cityRepository,
                simulationClockMutationExecutor: mutationExecutor,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);

            bool result = await handler.Handle(
                request: new ArchiveCityCommand(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.True(city.IsArchived);
            Assert.True(instance.IsArchived);
            Assert.Equal(
                expected: archivedAtUtc,
                actual: city.ArchivedAtUtc);
            Assert.Equal(
                expected: archivedAtUtc,
                actual: instance.ArchivedAtUtc);
            Assert.Equal(
                expected: city.Id.Value,
                actual: instanceRepository.RequestedSimulationId!.Value.Value);
            Assert.Equal(
                expected: ClockState.Paused,
                actual: clock.State);
            Assert.Equal(
                expected: city.Id.Value,
                actual: mutationExecutor.RequestedSimulationId!.Value.Value);
            Assert.True(mutationExecutor.RequestedAllowArchivedHost);
            Assert.Equal(
                expected: 2,
                actual: cityRepository.GetByIdCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            IDomainEvent simulationEvent = Assert.Single(outboxWriter.SimulationEvents);
            SimulationArchivedDomainEvent simulationArchivedEvent =
                Assert.IsType<SimulationArchivedDomainEvent>(simulationEvent);
            Assert.Equal(
                expected: instance.Id,
                actual: simulationArchivedEvent.SimulationId);
            Assert.Empty(outboxWriter.CityEvents);
        }
    }
}
