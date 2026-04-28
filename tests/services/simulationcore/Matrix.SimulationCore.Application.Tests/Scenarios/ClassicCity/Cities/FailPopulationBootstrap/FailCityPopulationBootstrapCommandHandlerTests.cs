using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailPopulationBootstrap;

public sealed class FailCityPopulationBootstrapCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var handler = new FailCityPopulationBootstrapCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

        var result = await handler.Handle(
            new FailCityPopulationBootstrapCommand(Guid.NewGuid(), Guid.NewGuid(), "TIMEOUT"),
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenOperationMatches_FailsBootstrapAndSaves()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2048-06-02T08:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true, requiresEconomyBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository { CityById = city };
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new FailCityPopulationBootstrapCommandHandler(
            cityRepository,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(failedAtUtc));

        var result = await handler.Handle(
            new FailCityPopulationBootstrapCommand(city.Id.Value, city.PopulationBootstrapOperationId, "network_down"),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(failedAtUtc, city.PopulationBootstrapFailedAtUtc);
        Assert.Equal("NETWORK_DOWN", city.PopulationBootstrapFailureCode);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
