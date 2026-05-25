using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompletePopulationBootstrap
{
    public sealed class CompleteCityPopulationBootstrapCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
        {
            var handler = new CompleteCityPopulationBootstrapCommandHandler(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                unitOfWork: new ApplicationTestSupport.FakeUnitOfWork(),
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(
                    DateTimeOffset.Parse("2048-06-01T12:00:00+00:00")));

            bool result = await handler.Handle(
                request: new CompleteCityPopulationBootstrapCommand(
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
            var handler = new CompleteCityPopulationBootstrapCommandHandler(
                cityRepository: cityRepository,
                unitOfWork: unitOfWork,
                timeProvider: new ApplicationTestSupport.FixedTimeProvider(completedAtUtc));

            bool result = await handler.Handle(
                request: new CompleteCityPopulationBootstrapCommand(
                    CityId: city.Id.Value,
                    OperationId: city.PopulationBootstrapOperationId),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: completedAtUtc,
                actual: city.PopulationBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
