using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Weather
{
    public sealed class WeatherAdvanceExecutorTests
    {
        [Fact]
        public async Task AdvanceAsync_WhenCityDoesNotExist_ReturnsNullWithoutLoadingWeather()
        {
            CityId cityId = new(Guid.NewGuid());
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
            var bootstrapFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    _,
                    _) => throw new InvalidOperationException("Bootstrap should not run without a city.")
            };
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => throw new InvalidOperationException("Planner should not run without a city.")
            };
            var executor = new WeatherAdvanceExecutor(
                cityRepository: cityRepository,
                weatherRepository: weatherRepository,
                bootstrapFactory: bootstrapFactory,
                planner: planner);

            CityWeather? result = await executor.AdvanceAsync(
                cityId: cityId,
                evaluatedAt: SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T09:07:08+00:00")),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: cityId,
                actual: cityRepository.RequestedCityId);
            Assert.Null(weatherRepository.RequestedCityId);
            Assert.Null(weatherRepository.AddedWeather);
            Assert.Null(bootstrapFactory.RequestedCity);
            Assert.Null(planner.RequestedEvaluatedAt);
        }

        [Fact]
        public async Task AdvanceAsync_WhenWeatherDoesNotExist_BootstrapsAndStoresInitialWeather()
        {
            City city = ClassicCityTestSupport.CreateCity();
            var evaluatedAt = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T09:07:08+00:00"));
            CityWeather initialWeather = WeatherTestSupport.CreateCityWeather(city.Id);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
            var bootstrapFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    _,
                    _) => initialWeather
            };
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => throw new InvalidOperationException("Planner should not run for bootstrap.")
            };
            var executor = new WeatherAdvanceExecutor(
                cityRepository: cityRepository,
                weatherRepository: weatherRepository,
                bootstrapFactory: bootstrapFactory,
                planner: planner);

            CityWeather? result = await executor.AdvanceAsync(
                cityId: city.Id,
                evaluatedAt: evaluatedAt,
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: initialWeather,
                actual: result);
            Assert.Equal(
                expected: city.Id,
                actual: weatherRepository.RequestedCityId);
            Assert.Same(
                expected: initialWeather,
                actual: weatherRepository.AddedWeather);
            Assert.Equal(
                expected: city,
                actual: bootstrapFactory.RequestedCity);
            Assert.Equal(
                expected: evaluatedAt,
                actual: bootstrapFactory.RequestedInitialTime);
            Assert.Null(planner.RequestedEvaluatedAt);
        }

        [Fact]
        public async Task AdvanceAsync_WhenWeatherExistsWithoutOverride_UsesCurrentStateAsPlannerBaseline()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityWeather cityWeather = WeatherTestSupport.CreateCityWeather(city.Id);
            WeatherState previousState = cityWeather.CurrentState;
            cityWeather.ClearDomainEvents();
            var evaluatedAt = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T10:07:08+00:00"));
            WeatherState nextState = CreateWeatherState(
                type: WeatherType.Rain,
                severity: WeatherSeverity.Moderate,
                startedAt: SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T09:07:08+00:00")),
                temperature: 13m);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository
            {
                WeatherByCityId = cityWeather
            };
            var bootstrapFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    _,
                    _) => throw new InvalidOperationException("Bootstrap should not run for existing weather.")
            };
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => nextState
            };
            var executor = new WeatherAdvanceExecutor(
                cityRepository: cityRepository,
                weatherRepository: weatherRepository,
                bootstrapFactory: bootstrapFactory,
                planner: planner);

            CityWeather? result = await executor.AdvanceAsync(
                cityId: city.Id,
                evaluatedAt: evaluatedAt,
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: cityWeather,
                actual: result);
            Assert.Equal(
                expected: city.Environment,
                actual: planner.RequestedEnvironment);
            Assert.Equal(
                expected: cityWeather.ClimateProfile,
                actual: planner.RequestedClimateProfile);
            Assert.Equal(
                expected: city.GenerationSeed,
                actual: planner.RequestedGenerationSeed);
            Assert.Equal(
                expected: evaluatedAt,
                actual: planner.RequestedEvaluatedAt);
            Assert.Equal(
                expected: previousState,
                actual: planner.RequestedPreviousState);
            Assert.Equal(
                expected: nextState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: evaluatedAt,
                actual: cityWeather.LastEvaluatedAt);
            CityWeatherChangedDomainEvent changedEvent =
                Assert.IsType<CityWeatherChangedDomainEvent>(Assert.Single(cityWeather.DomainEvents));
            Assert.Equal(
                expected: city.Id,
                actual: changedEvent.CityId);
            Assert.Equal(
                expected: nextState,
                actual: changedEvent.CurrentState);
        }

        [Fact]
        public async Task AdvanceAsync_WhenOverrideIsActive_PassesNullBaselineAndKeepsForcedState()
        {
            City city = ClassicCityTestSupport.CreateCity();
            CityWeather cityWeather = WeatherTestSupport.CreateCityWeather(city.Id);
            WeatherState overrideState = CreateWeatherState(
                type: WeatherType.Storm,
                severity: WeatherSeverity.Severe,
                startedAt: SimTime.FromUtc(WeatherTestSupport.SimTimeUtc),
                temperature: 3m,
                duration: TimeSpan.FromHours(5));
            cityWeather.StartOverride(
                forcedState: overrideState,
                source: WeatherOverrideSource.System,
                reason: "Storm front");
            cityWeather.ClearDomainEvents();
            var evaluatedAt = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T08:07:08+00:00"));
            WeatherState naturalState = CreateWeatherState(
                type: WeatherType.Clear,
                severity: WeatherSeverity.Calm,
                startedAt: SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T08:00:00+00:00")),
                temperature: 17m);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var weatherRepository = new WeatherTestSupport.FakeCityWeatherRepository
            {
                WeatherByCityId = cityWeather
            };
            var bootstrapFactory = new BootstrapTestSupport.FakeCityWeatherBootstrapFactory
            {
                Factory = (
                    _,
                    _) => throw new InvalidOperationException("Bootstrap should not run for existing weather.")
            };
            var planner = new WeatherTestSupport.FakeWeatherStatePlanner
            {
                Planner = (
                    _,
                    _,
                    _,
                    _,
                    _) => naturalState
            };
            var executor = new WeatherAdvanceExecutor(
                cityRepository: cityRepository,
                weatherRepository: weatherRepository,
                bootstrapFactory: bootstrapFactory,
                planner: planner);

            CityWeather? result = await executor.AdvanceAsync(
                cityId: city.Id,
                evaluatedAt: evaluatedAt,
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: cityWeather,
                actual: result);
            Assert.Null(planner.RequestedPreviousState);
            Assert.Equal(
                expected: overrideState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: evaluatedAt,
                actual: cityWeather.LastEvaluatedAt);
            Assert.NotNull(cityWeather.ActiveOverride);
            Assert.Empty(cityWeather.DomainEvents);
        }

        private static WeatherState CreateWeatherState(
            WeatherType type,
            WeatherSeverity severity,
            SimTime startedAt,
            decimal temperature,
            TimeSpan? duration = null)
        {
            TimeSpan actualDuration = duration ?? TimeSpan.FromHours(3);

            return WeatherState.Create(
                type: type,
                severity: severity,
                precipitationKind: type is WeatherType.Clear
                    ? PrecipitationKind.None
                    : PrecipitationKind.Rain,
                temperature: TemperatureC.From(temperature),
                humidity: HumidityPercent.From(56m),
                windSpeed: WindSpeedKph.From(18m),
                cloudCoverage: CloudCoveragePercent.From(
                    type is WeatherType.Clear
                        ? 8m
                        : 64m),
                pressure: PressureHpa.From(1009m),
                startedAt: startedAt,
                expectedUntil: startedAt.Add(actualDuration));
        }
    }
}
