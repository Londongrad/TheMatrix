using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompletePopulationBootstrap;

public sealed class CompleteCityPopulationBootstrapCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var handler = new CompleteCityPopulationBootstrapCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-01T12:00:00+00:00")));

        var result = await handler.Handle(
            new CompleteCityPopulationBootstrapCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenOperationMatches_CompletesBootstrapAndSaves()
    {
        DateTimeOffset completedAtUtc = DateTimeOffset.Parse("2048-06-01T12:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true, requiresEconomyBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository { CityById = city };
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new CompleteCityPopulationBootstrapCommandHandler(
            cityRepository,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(completedAtUtc));

        var result = await handler.Handle(
            new CompleteCityPopulationBootstrapCommand(city.Id.Value, city.PopulationBootstrapOperationId),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(completedAtUtc, city.PopulationBootstrapCompletedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
