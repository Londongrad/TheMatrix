using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Simulation;

public sealed class ClassicCitySimulationAdvanceHandlerTests
{
    [Fact]
    public async Task HandleAdvancedAsync_EmitsAdvanceTimeAndClassicCityPhaseWatermarks()
    {
        SimulationHost host = CreateHost();
        SimulationTimeAdvancedDomainEvent advancedEvent = CreateAdvancedEvent(host.SimulationId, new CityId(host.HostId.Value));
        var weatherAdvanceExecutor = new FakeWeatherAdvanceExecutor();
        var activeTripAdvanceExecutor = new FakeCityActiveTripAdvanceExecutor();
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var handler = new ClassicCitySimulationAdvanceHandler(weatherAdvanceExecutor, activeTripAdvanceExecutor, outboxWriter);

        await handler.HandleAdvancedAsync(host, advancedEvent, CancellationToken.None);

        Assert.Equal(new CityId(host.HostId.Value), weatherAdvanceExecutor.RequestedCityId);
        Assert.Equal(advancedEvent.To, weatherAdvanceExecutor.RequestedEvaluatedAt);
        Assert.Equal(new CityId(host.HostId.Value), activeTripAdvanceExecutor.RequestedCityId);
        Assert.Equal(advancedEvent.From.ValueUtc, activeTripAdvanceExecutor.RequestedFromSimTimeUtc);
        Assert.Equal(advancedEvent.To.ValueUtc, activeTripAdvanceExecutor.RequestedToSimTimeUtc);
        Assert.Equal(advancedEvent.TickId.Value, activeTripAdvanceExecutor.RequestedTickId);

        ClassicCityTestSupport.FakeSimulationCoreOutboxWriter.CityTimeAdvancedCall timeAdvancedCall =
            Assert.Single(outboxWriter.CityTimeAdvancedCalls);
        Assert.Equal(new CityId(host.HostId.Value), timeAdvancedCall.CityId);
        Assert.Equal(host.SimulationId, timeAdvancedCall.SimulationId);
        Assert.Equal(host.SimulationKind, timeAdvancedCall.SimulationKind);
        Assert.Equal(advancedEvent.From, timeAdvancedCall.From);
        Assert.Equal(advancedEvent.To, timeAdvancedCall.To);
        Assert.Equal(advancedEvent.TickId, timeAdvancedCall.TickId);
        Assert.Equal(advancedEvent.Speed, timeAdvancedCall.Speed);
        Assert.Equal(CityTickPhase.AdvanceTime, timeAdvancedCall.Phase);

        Assert.Empty(outboxWriter.WeatherEvents);
        Assert.Equal(
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
            outboxWriter.CityTickPhaseReachedCalls.Select(static x => x.Phase).ToArray());
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
        var handler = new ClassicCitySimulationAdvanceHandler(weatherAdvanceExecutor, activeTripAdvanceExecutor, outboxWriter);

        await handler.HandleAdvancedAsync(
            host,
            CreateAdvancedEvent(host.SimulationId, cityId),
            CancellationToken.None);

        CityWeatherCreatedDomainEvent publishedEvent =
            Assert.IsType<CityWeatherCreatedDomainEvent>(Assert.Single(outboxWriter.WeatherEvents));
        Assert.Equal(cityId, publishedEvent.CityId);
        Assert.Empty(cityWeather.DomainEvents);
    }

    private static SimulationHost CreateHost()
    {
        CityId cityId = new(Guid.NewGuid());

        return new SimulationHost(
            SimulationId: new SimulationId(Guid.NewGuid()),
            HostId: new SimulationHostId(cityId.Value),
            HostKind: SimulationHostKind.City,
            SimulationKind: SimulationKind.ClassicCity,
            State: SimulationHostState.Active,
            CreatedAtUtc: DateTimeOffset.Parse("2048-04-05T06:07:08+00:00"),
            ArchivedAtUtc: null);
    }

    private static SimulationTimeAdvancedDomainEvent CreateAdvancedEvent(
        SimulationId simulationId,
        CityId cityId)
    {
        SimTime from = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T06:07:08+00:00"));
        SimTime to = SimTime.FromUtc(DateTimeOffset.Parse("2048-04-05T06:08:08+00:00"));

        return new SimulationTimeAdvancedDomainEvent(
            SimulationId: simulationId,
            CityId: cityId,
            From: from,
            To: to,
            TickId: TickId.Start().Next(),
            Speed: SimSpeed.From(60m));
    }

    private sealed class FakeWeatherAdvanceExecutor : IWeatherAdvanceExecutor
    {
        public CityId? RequestedCityId { get; private set; }
        public SimTime? RequestedEvaluatedAt { get; private set; }
        public CityWeather? Result { get; init; }

        public Task<CityWeather?> AdvanceAsync(CityId cityId, SimTime evaluatedAt, CancellationToken cancellationToken)
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
