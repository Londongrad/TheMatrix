using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.DeleteCity
{
    public sealed class DeleteCityCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsNotFound()
        {
            var handler = new DeleteCityCommandHandler(
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository(),
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                outboxWriter: new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

            DeleteCityResult result = await handler.Handle(
                request: new DeleteCityCommand(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityResult.NotFound,
                actual: result);
        }

        [Fact]
        public async Task Handle_WhenCityIsNotArchived_ReturnsNotAllowed()
        {
            City city = ClassicCityTestSupport.CreateCity();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var handler = new DeleteCityCommandHandler(
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository(),
                cityRepository: cityRepository,
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                outboxWriter: new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-01T10:00:00+00:00")));

            DeleteCityResult result = await handler.Handle(
                request: new DeleteCityCommand(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityResult.NotAllowed,
                actual: result);
        }

        [Fact]
        public async Task Handle_WhenCityIsArchived_DeletesClockWritesOutboxAndRemovesCity()
        {
            var deletedAtUtc = DateTimeOffset.Parse("2048-06-01T10:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity();
            SimulationInstance instance = SimulationTestSupport.CreateInstance(city);
            city.Archive(deletedAtUtc.AddHours(-1));
            instance.Archive(deletedAtUtc.AddHours(-1));
            city.ClearDomainEvents();
            instance.ClearDomainEvents();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var instanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository
            {
                InstanceById = instance
            };
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new DeleteCityCommandHandler(
                simulationInstanceRepository: instanceRepository,
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(deletedAtUtc));

            DeleteCityResult result = await handler.Handle(
                request: new DeleteCityCommand(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityResult.Deleted,
                actual: result);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: city.Id.Value,
                actual: clockRepository.DeletedSimulationId!.Value.Value);
            Assert.Same(
                expected: instance,
                actual: instanceRepository.DeletedInstance);
            Assert.Same(
                expected: city,
                actual: cityRepository.DeletedCity);
            IDomainEvent simulationEvent = Assert.Single(outboxWriter.SimulationEvents);
            SimulationDeletedDomainEvent simulationDeletedEvent =
                Assert.IsType<SimulationDeletedDomainEvent>(simulationEvent);
            Assert.Equal(
                expected: instance.Id,
                actual: simulationDeletedEvent.SimulationId);
            IDomainEvent domainEvent = Assert.Single(outboxWriter.CityEvents);
            CityDeletedDomainEvent deletedEvent = Assert.IsType<CityDeletedDomainEvent>(domainEvent);
            Assert.Equal(
                expected: city.Id,
                actual: deletedEvent.CityId);
            Assert.Equal(
                expected: deletedAtUtc,
                actual: deletedEvent.DeletedAtUtc);
        }
    }
}
