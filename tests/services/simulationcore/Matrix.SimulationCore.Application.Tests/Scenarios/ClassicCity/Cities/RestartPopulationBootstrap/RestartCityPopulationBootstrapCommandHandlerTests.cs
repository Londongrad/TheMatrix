using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RestartPopulationBootstrap
{
    public sealed class RestartCityPopulationBootstrapCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsNotFound()
        {
            var handler = new RestartCityPopulationBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

            RestartCityPopulationBootstrapResult result = await handler.Handle(
                request: new RestartCityPopulationBootstrapCommand(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RestartCityPopulationBootstrapStatus.NotFound,
                actual: result.Status);
        }

        [Fact]
        public async Task Handle_WhenCityIsNotProvisioningFailed_ReturnsNotAllowed()
        {
            City city = ClassicCityTestSupport.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var handler = new RestartCityPopulationBootstrapCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

            RestartCityPopulationBootstrapResult result = await handler.Handle(
                request: new RestartCityPopulationBootstrapCommand(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RestartCityPopulationBootstrapStatus.NotAllowed,
                actual: result.Status);
        }

        [Fact]
        public async Task Handle_WhenCityHasFailedPopulationBootstrap_RestartsAndSaves()
        {
            var restartedAtUtc = DateTimeOffset.Parse("2048-06-02T08:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            city.TryFailPopulationBootstrap(
                operationId: city.PopulationBootstrapOperationId,
                failureCode: "TIMEOUT",
                failedAtUtc: restartedAtUtc.AddHours(-1));
            city.ClearDomainEvents();
            Guid previousPopulationOperationId = city.PopulationBootstrapOperationId;
            Guid previousEconomyOperationId = city.EconomyBootstrapOperationId;
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var handler = new RestartCityPopulationBootstrapCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(restartedAtUtc));

            RestartCityPopulationBootstrapResult result = await handler.Handle(
                request: new RestartCityPopulationBootstrapCommand(
                    CityId: city.Id.Value,
                    PlannedPeopleCountOverride: 32000),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RestartCityPopulationBootstrapStatus.Restarted,
                actual: result.Status);
            Assert.NotNull(result.PopulationBootstrapOperationId);
            Assert.NotNull(result.EconomyBootstrapOperationId);
            Assert.NotEqual(
                expected: previousPopulationOperationId,
                actual: result.PopulationBootstrapOperationId!.Value);
            Assert.NotEqual(
                expected: previousEconomyOperationId,
                actual: result.EconomyBootstrapOperationId!.Value);
            Assert.Equal(
                expected: "ClassicCity",
                actual: result.SimulationKind);
            Assert.Equal(
                expected: 32000,
                actual: city.GenerationProfile.PlannedPeopleCount);
            Assert.Equal(
                expected: restartedAtUtc,
                actual: city.ProvisioningStartedAtUtc);
            Assert.Equal(
                expected: restartedAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
