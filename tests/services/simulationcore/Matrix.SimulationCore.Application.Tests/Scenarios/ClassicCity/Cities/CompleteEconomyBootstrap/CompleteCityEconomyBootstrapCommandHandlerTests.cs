using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var handler = new CompleteCityEconomyBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-01T12:00:00+00:00")));

            bool result = await handler.Handle(
                request: new CompleteCityEconomyBootstrapCommand(
                    CityId: Guid.NewGuid(),
                    OperationId: Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_WhenOperationMatches_CompletesBootstrapAndSaves()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-06-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var simulationInstanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository
            {
                InstanceById = SimulationTestSupport.CreateInstance(city)
            };
            var handler = new CompleteCityEconomyBootstrapCommandHandler(
                cityRepository: cityRepository,
                simulationInstanceRepository: simulationInstanceRepository,
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(completedAtUtc));

            bool result = await handler.Handle(
                request: new CompleteCityEconomyBootstrapCommand(
                    CityId: city.Id.Value,
                    OperationId: city.EconomyBootstrapOperationId),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: completedAtUtc,
                actual: city.EconomyBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handle_WhenEconomyIsLastBootstrap_ActivatesRuntimeInstance()
        {
            City city = ClassicCityTestSupport.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            city.TryCompletePopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                completedAtUtc: city.CreatedAtUtc.AddMinutes(1));
            SimulationInstance instance = SimulationTestSupport.CreateInstance(city);
            var instanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository
            {
                InstanceById = instance
            };
            var handler = new CompleteCityEconomyBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository { CityById = city },
                simulationInstanceRepository: instanceRepository,
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    city.CreatedAtUtc.AddMinutes(2)));

            bool result = await handler.Handle(
                request: new CompleteCityEconomyBootstrapCommand(
                    CityId: city.Id.Value,
                    OperationId: city.EconomyBootstrapOperationId),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.True(instance.IsActive);
            Assert.Equal(ClassicCityRuntime.Key, instanceRepository.RequestedRuntimeKey);
            Assert.Equal(city.Id.Value, instanceRepository.RequestedHostId?.Value);
        }
    }
}
