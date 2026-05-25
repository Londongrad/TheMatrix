using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
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
            var handler = new CompleteCityEconomyBootstrapCommandHandler(
                cityRepository: cityRepository,
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
    }
}
