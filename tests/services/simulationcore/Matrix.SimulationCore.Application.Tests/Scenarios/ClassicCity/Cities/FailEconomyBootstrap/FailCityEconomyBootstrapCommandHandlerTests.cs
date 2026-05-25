using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailEconomyBootstrap
{
    public sealed class FailCityEconomyBootstrapCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var handler = new FailCityEconomyBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

            bool result = await handler.Handle(
                request: new FailCityEconomyBootstrapCommand(
                    CityId: Guid.NewGuid(),
                    OperationId: Guid.NewGuid(),
                    FailureCode: "CAPACITY_LIMIT"),
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
            var handler = new FailCityEconomyBootstrapCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(failedAtUtc));

            bool result = await handler.Handle(
                request: new FailCityEconomyBootstrapCommand(
                    CityId: city.Id.Value,
                    OperationId: city.EconomyBootstrapOperationId,
                    FailureCode: "capacity_limit"),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: failedAtUtc,
                actual: city.EconomyBootstrapFailedAtUtc);
            Assert.Equal(
                expected: "CAPACITY_LIMIT",
                actual: city.EconomyBootstrapFailureCode);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
