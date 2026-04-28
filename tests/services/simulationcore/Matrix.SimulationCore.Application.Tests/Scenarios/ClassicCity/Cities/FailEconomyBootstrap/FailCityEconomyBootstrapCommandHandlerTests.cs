using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.FailEconomyBootstrap;

public sealed class FailCityEconomyBootstrapCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var handler = new FailCityEconomyBootstrapCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

        var result = await handler.Handle(
            new FailCityEconomyBootstrapCommand(Guid.NewGuid(), Guid.NewGuid(), "CAPACITY_LIMIT"),
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
        var handler = new FailCityEconomyBootstrapCommandHandler(
            cityRepository,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(failedAtUtc));

        var result = await handler.Handle(
            new FailCityEconomyBootstrapCommand(city.Id.Value, city.EconomyBootstrapOperationId, "capacity_limit"),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(failedAtUtc, city.EconomyBootstrapFailedAtUtc);
        Assert.Equal("CAPACITY_LIMIT", city.EconomyBootstrapFailureCode);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
