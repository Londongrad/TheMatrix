using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var handler = new FailCityPopulationBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

            bool result = await handler.Handle(
                request: new FailCityPopulationBootstrapCommand(
                    CityId: Guid.NewGuid(),
                    OperationId: Guid.NewGuid(),
                    FailureCode: "TIMEOUT"),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
        }

        [Fact]
        public async Task Handle_WhenOperationMatches_FailsBootstrapAndSaves()
        {
            var failedAtUtc = DateTimeOffset.Parse("2048-06-02T08:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            SimulationInstance instance = SimulationTestSupport.CreateInstance(city);
            var handler = new FailCityPopulationBootstrapCommandHandler(
                cityRepository: cityRepository,
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository
                {
                    InstanceById = instance
                },
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(failedAtUtc));

            bool result = await handler.Handle(
                request: new FailCityPopulationBootstrapCommand(
                    CityId: city.Id.Value,
                    OperationId: city.PopulationBootstrapOperationId,
                    FailureCode: "network_down"),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: failedAtUtc,
                actual: city.PopulationBootstrapFailedAtUtc);
            Assert.Equal(
                expected: "NETWORK_DOWN",
                actual: city.PopulationBootstrapFailureCode);
            Assert.Equal(
                expected: SimulationHostState.ProvisioningFailed,
                actual: instance.State);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
