using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public sealed class RecalculateCityEnvironmentalConditionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenStateIsMissing_ReturnsNotInitialized()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            RecalculateCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RecalculateCityEnvironmentalConditionsStatus.NotInitialized,
                actual: result.Status);
            Assert.Equal(
                expected: 0m,
                actual: result.FloodingIndex);
        }

        [Fact]
        public async Task Handle_WhenTimestampIsStale_ReturnsStale()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            RecalculateCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: new FakeUnitOfWork());

            RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(atUtc: SimulationSystemsApplicationTestSupport.CreatedAtUtc.AddMinutes(-1)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RecalculateCityEnvironmentalConditionsStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: state.FloodingIndex.Value,
                actual: result.FloodingIndex);
        }

        [Fact]
        public async Task Handle_WhenTimestampMatchesCurrentState_ReturnsDuplicateAndPersistsWeatherPressure()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            RecalculateCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(atUtc: state.LastEvaluatedAtUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RecalculateCityEnvironmentalConditionsStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.True(state.WeatherPressure.StormPressure > 0m);
        }

        [Fact]
        public async Task Handle_WhenRecalculationSucceeds_AppliesSnapshot()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var unitOfWork = new FakeUnitOfWork();
            RecalculateCityEnvironmentalConditionsCommandHandler handler = CreateHandler(
                repository: repository,
                unitOfWork: unitOfWork);

            RecalculateCityEnvironmentalConditionsResult result = await handler.Handle(
                request: CreateCommand(atUtc: SimulationSystemsApplicationTestSupport.LaterUtc),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: RecalculateCityEnvironmentalConditionsStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.LaterUtc,
                actual: state.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: state.FloodingIndex.Value,
                actual: result.FloodingIndex);
            Assert.True(state.WeatherPressure.RainPressure > 0m);
        }

        private static RecalculateCityEnvironmentalConditionsCommandHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeUnitOfWork unitOfWork)
        {
            return new RecalculateCityEnvironmentalConditionsCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                policy: new CityEnvironmentalConditionPolicy(),
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory());
        }

        private static RecalculateCityEnvironmentalConditionsCommand CreateCommand(DateTimeOffset? atUtc = null)
        {
            return new RecalculateCityEnvironmentalConditionsCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                AtSimTimeUtc: atUtc ?? SimulationSystemsApplicationTestSupport.LaterUtc,
                Weather: SimulationSystemsApplicationTestSupport.CreateWeather());
        }
    }
}
