using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;

public sealed class RecalculateCityEnvironmentalConditionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(RecalculateCityEnvironmentalConditionsStatus.NotInitialized, result.Status);
        Assert.Equal(0m, result.FloodingIndex);
    }

    [Fact]
    public async Task Handle_WhenTimestampIsStale_ReturnsStale()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(atUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddMinutes(-1)),
            CancellationToken.None);

        Assert.Equal(RecalculateCityEnvironmentalConditionsStatus.Stale, result.Status);
        Assert.Equal(state.FloodingIndex.Value, result.FloodingIndex);
    }

    [Fact]
    public async Task Handle_WhenTimestampMatchesCurrentState_ReturnsDuplicateAndPersistsWeatherPressure()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(atUtc: state.LastEvaluatedAtUtc),
            CancellationToken.None);

        Assert.Equal(RecalculateCityEnvironmentalConditionsStatus.Duplicate, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.True(state.WeatherPressure.StormPressure > 0m);
    }

    [Fact]
    public async Task Handle_WhenRecalculationSucceeds_AppliesSnapshot()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
            CreateCommand(atUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
            CancellationToken.None);

        Assert.Equal(RecalculateCityEnvironmentalConditionsStatus.Applied, result.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(SimulationSystemsApplicationTestSupport.LaterUtc, state.LastEvaluatedAtUtc);
        Assert.Equal(state.FloodingIndex.Value, result.FloodingIndex);
        Assert.True(state.WeatherPressure.RainPressure > 0m);
    }

    private static RecalculateCityEnvironmentalConditionsCommandHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        return new RecalculateCityEnvironmentalConditionsCommandHandler(
            repository,
            unitOfWork,
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());
    }

    private static RecalculateCityEnvironmentalConditionsCommand CreateCommand(DateTimeOffset? atUtc = null)
    {
        return new RecalculateCityEnvironmentalConditionsCommand(
            CityId: SimulationSystemsApplicationTestSupport.CityId,
            AtSimTimeUtc: atUtc ?? SimulationSystemsApplicationTestSupport.LaterUtc,
            Weather: SimulationSystemsApplicationTestSupport.CreateWeather());
    }
}
