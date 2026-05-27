using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Simulation
{
    public sealed class ClassicCitySimulationAdvanceHandlerTests
    {
        [Fact]
        public async Task HandleAdvancedAsync_EmitsAdvanceTimeAndClassicCityPhaseWatermarks()
        {
            SimulationHost host = CreateHost();
            SimulationTimeAdvancedDomainEvent advancedEvent = CreateAdvancedEvent(
                simulationId: host.SimulationId);
            var weatherAdvanceExecutor = new FakeWeatherAdvanceExecutor();
            var activeTripAdvanceExecutor = new FakeCityActiveTripAdvanceExecutor();
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var handler = new ClassicCitySimulationAdvanceHandler(
                weatherAdvanceExecutor: weatherAdvanceExecutor,
                activeTripAdvanceExecutor: activeTripAdvanceExecutor,
                outboxWriter: outboxWriter);

            await handler.HandleAdvancedAsync(
                host: host,
                advancedEvent: advancedEvent,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: new CityId(host.HostId.Value),
                actual: weatherAdvanceExecutor.RequestedCityId);
            Assert.Equal(
                expected: advancedEvent.To,
                actual: weatherAdvanceExecutor.RequestedEvaluatedAt);
            Assert.Equal(
                expected: new CityId(host.HostId.Value),
                actual: activeTripAdvanceExecutor.RequestedCityId);
            Assert.Equal(
                expected: advancedEvent.From.ValueUtc,
                actual: activeTripAdvanceExecutor.RequestedFromSimTimeUtc);
            Assert.Equal(
                expected: advancedEvent.To.ValueUtc,
                actual: activeTripAdvanceExecutor.RequestedToSimTimeUtc);
            Assert.Equal(
                expected: advancedEvent.TickId.Value,
                actual: activeTripAdvanceExecutor.RequestedTickId);

            ClassicCityTestSupport.FakeSimulationCoreOutboxWriter.CityTimeAdvancedCall timeAdvancedCall =
                Assert.Single(outboxWriter.CityTimeAdvancedCalls);
            Assert.Equal(
                expected: new CityId(host.HostId.Value),
                actual: timeAdvancedCall.CityId);
            Assert.Equal(
                expected: host.SimulationId,
                actual: timeAdvancedCall.SimulationId);
            Assert.Equal(
                expected: host.SimulationKind,
                actual: timeAdvancedCall.SimulationKind);
            Assert.Equal(
                expected: advancedEvent.From,
                actual: timeAdvancedCall.From);
            Assert.Equal(
                expected: advancedEvent.To,
                actual: timeAdvancedCall.To);
            Assert.Equal(
                expected: advancedEvent.TickId,
                actual: timeAdvancedCall.TickId);
            Assert.Equal(
                expected: advancedEvent.Speed,
                actual: timeAdvancedCall.Speed);
            Assert.Equal(
                expected: CityTickPhase.AdvanceTime,
                actual: timeAdvancedCall.Phase);

            Assert.Empty(outboxWriter.WeatherEvents);
            Assert.Equal(
                expected:
                [
                    CityTickPhase.SystemsDegradation,
                    CityTickPhase.IncidentGeneration,
                    CityTickPhase.DispatchExecution,
                    CityTickPhase.ResourceSettlement,
                    CityTickPhase.BudgetSettlement,
                    CityTickPhase.PopulationReaction,
                    CityTickPhase.Projection,
                    CityTickPhase.TickCompleted
                ],
                actual: outboxWriter.CityTickPhaseReachedCalls.Select(static x => x.Phase)
                   .ToArray());
        }

        [Fact]
        public async Task HandleAdvancedAsync_WhenWeatherProducesDomainEvents_PublishesAndClearsThem()
        {
            SimulationHost host = CreateHost();
            var cityId = new CityId(host.HostId.Value);
            CityWeather cityWeather = WeatherTestSupport.CreateCityWeather(cityId);
            var weatherAdvanceExecutor = new FakeWeatherAdvanceExecutor
            {
                Result = cityWeather
            };
            var activeTripAdvanceExecutor = new FakeCityActiveTripAdvanceExecutor();
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var handler = new ClassicCitySimulationAdvanceHandler(
                weatherAdvanceExecutor: weatherAdvanceExecutor,
                activeTripAdvanceExecutor: activeTripAdvanceExecutor,
                outboxWriter: outboxWriter);

            await handler.HandleAdvancedAsync(
                host: host,
                advancedEvent: CreateAdvancedEvent(
                    simulationId: host.SimulationId),
                cancellationToken: CancellationToken.None);

            CityWeatherCreatedDomainEvent publishedEvent =
                Assert.IsType<CityWeatherCreatedDomainEvent>(Assert.Single(outboxWriter.WeatherEvents));
            Assert.Equal(
                expected: cityId,
                actual: publishedEvent.CityId);
            Assert.Empty(cityWeather.DomainEvents);
        }

        private static SimulationHost CreateHost()
        {
            CityId cityId = new(Guid.NewGuid());

            return new SimulationHost(
                SimulationId: new SimulationId(Guid.NewGuid()),
                HostId: new SimulationHostId(cityId.Value),
                RuntimeKey: ClassicCityRuntime.Key,
                HostKind: SimulationHostKind.City,
                SimulationKind: SimulationKind.ClassicCity,
                State: SimulationHostState.Active,
                CreatedAtUtc: DateTimeOffset.Parse("2048-04-05T06:07:08+00:00"),
                ArchivedAtUtc: null);
        }

        private static SimulationTimeAdvancedDomainEvent CreateAdvancedEvent(
            SimulationId simulationId)
        {
            var from = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T06:07:08+00:00"));
            var to = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T06:08:08+00:00"));

            return new SimulationTimeAdvancedDomainEvent(
                SimulationId: simulationId,
                From: from,
                To: to,
                TickId: TickId.Start()
                   .Next(),
                Speed: SimSpeed.From(60m));
        }

        private sealed class FakeWeatherAdvanceExecutor : IWeatherAdvanceExecutor
        {
            public CityId? RequestedCityId { get; private set; }
            public SimTime? RequestedEvaluatedAt { get; private set; }
            public CityWeather? Result { get; init; }

            public Task<CityWeather?> AdvanceAsync(
                CityId cityId,
                SimTime evaluatedAt,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                RequestedEvaluatedAt = evaluatedAt;
                return Task.FromResult(Result);
            }
        }

        private sealed class FakeCityActiveTripAdvanceExecutor : ICityActiveTripAdvanceExecutor
        {
            public CityId? RequestedCityId { get; private set; }
            public DateTimeOffset? RequestedFromSimTimeUtc { get; private set; }
            public DateTimeOffset? RequestedToSimTimeUtc { get; private set; }
            public long? RequestedTickId { get; private set; }

            public Task AdvanceAsync(
                CityId cityId,
                DateTimeOffset fromSimTimeUtc,
                DateTimeOffset toSimTimeUtc,
                long tickId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                RequestedFromSimTimeUtc = fromSimTimeUtc;
                RequestedToSimTimeUtc = toSimTimeUtc;
                RequestedTickId = tickId;
                return Task.CompletedTask;
            }
        }
    }
}
