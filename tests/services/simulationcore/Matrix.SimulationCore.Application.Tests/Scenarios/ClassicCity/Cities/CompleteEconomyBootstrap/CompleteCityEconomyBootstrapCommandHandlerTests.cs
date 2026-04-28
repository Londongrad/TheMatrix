using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CompleteEconomyBootstrap;

public sealed class CompleteCityEconomyBootstrapCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsFalse()
    {
        var handler = new CompleteCityEconomyBootstrapCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-01T12:00:00+00:00")));

        var result = await handler.Handle(
            new CompleteCityEconomyBootstrapCommand(Guid.NewGuid(), Guid.NewGuid()),
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
        var handler = new CompleteCityEconomyBootstrapCommandHandler(
            cityRepository,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(completedAtUtc));

        var result = await handler.Handle(
            new CompleteCityEconomyBootstrapCommand(city.Id.Value, city.EconomyBootstrapOperationId),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(completedAtUtc, city.EconomyBootstrapCompletedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
