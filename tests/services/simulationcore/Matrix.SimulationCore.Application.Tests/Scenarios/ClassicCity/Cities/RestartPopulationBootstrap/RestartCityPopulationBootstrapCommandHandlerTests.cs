using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.RestartPopulationBootstrap;

public sealed class RestartCityPopulationBootstrapCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsNotFound()
    {
        var handler = new RestartCityPopulationBootstrapCommandHandler(
            new ClassicCityTestSupport.FakeCityRepository(),
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

        var result = await handler.Handle(new RestartCityPopulationBootstrapCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(RestartCityPopulationBootstrapStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenCityIsNotProvisioningFailed_ReturnsNotAllowed()
    {
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true, requiresEconomyBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository { CityById = city };
        var handler = new RestartCityPopulationBootstrapCommandHandler(
            cityRepository,
            new ApplicationTestSupport.FakeUnitOfWork(),
            new ApplicationTestSupport.FixedTimeProvider(DateTimeOffset.Parse("2048-06-02T08:00:00+00:00")));

        var result = await handler.Handle(new RestartCityPopulationBootstrapCommand(city.Id.Value), CancellationToken.None);

        Assert.Equal(RestartCityPopulationBootstrapStatus.NotAllowed, result.Status);
    }

    [Fact]
    public async Task Handle_WhenCityHasFailedPopulationBootstrap_RestartsAndSaves()
    {
        DateTimeOffset restartedAtUtc = DateTimeOffset.Parse("2048-06-02T08:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true, requiresEconomyBootstrap: true);
        city.TryFailPopulationBootstrap(
            city.PopulationBootstrapOperationId,
            "TIMEOUT",
            restartedAtUtc.AddHours(-1));
        city.ClearDomainEvents();
        Guid previousPopulationOperationId = city.PopulationBootstrapOperationId;
        Guid previousEconomyOperationId = city.EconomyBootstrapOperationId;
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository { CityById = city };
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new RestartCityPopulationBootstrapCommandHandler(
            cityRepository,
            unitOfWork,
            new ApplicationTestSupport.FixedTimeProvider(restartedAtUtc));

        var result = await handler.Handle(
            new RestartCityPopulationBootstrapCommand(city.Id.Value, PlannedPeopleCountOverride: 32000),
            CancellationToken.None);

        Assert.Equal(RestartCityPopulationBootstrapStatus.Restarted, result.Status);
        Assert.NotNull(result.PopulationBootstrapOperationId);
        Assert.NotNull(result.EconomyBootstrapOperationId);
        Assert.NotEqual(previousPopulationOperationId, result.PopulationBootstrapOperationId!.Value);
        Assert.NotEqual(previousEconomyOperationId, result.EconomyBootstrapOperationId!.Value);
        Assert.Equal("ClassicCity", result.SimulationKind);
        Assert.Equal(32000, city.GenerationProfile.PlannedPeopleCount);
        Assert.Equal(restartedAtUtc, city.ProvisioningStartedAtUtc);
        Assert.Equal(restartedAtUtc, city.ProvisioningHeartbeatAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
