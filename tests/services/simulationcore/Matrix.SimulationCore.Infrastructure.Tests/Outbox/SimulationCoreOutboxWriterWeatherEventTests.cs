using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterWeatherEventTests
{
    [Fact]
    public async Task AddWeatherEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddWeatherEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages));
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

        await writer.AddWeatherEventsAsync([], CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Empty(await dbContext.OutboxMessages.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AddWeatherEventsAsync_WhenCreatedAndChangedEventsAreAdded_WritesMatchingMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddWeatherEventsAsync_WhenCreatedAndChangedEventsAreAdded_WritesMatchingMessages));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(55);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var cityId = new CityId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        SimTime createdAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(2));
        SimTime changedAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(3));
        var createdEvent = new CityWeatherCreatedDomainEvent(
            CityId: cityId,
            InitialState: OutboxTestSupport.CreateWeatherState(
                startedAt: createdAt,
                expectedUntil: createdAt.Add(TimeSpan.FromHours(2)),
                type: WeatherType.Clear,
                precipitationKind: PrecipitationKind.None,
                severity: WeatherSeverity.Calm),
            ClimateProfile: OutboxTestSupport.CreateClimateProfile(),
            AtSimTime: createdAt);
        var changedEvent = new CityWeatherChangedDomainEvent(
            CityId: cityId,
            PreviousState: OutboxTestSupport.CreateWeatherState(
                startedAt: createdAt,
                expectedUntil: createdAt.Add(TimeSpan.FromHours(2)),
                type: WeatherType.Clear,
                precipitationKind: PrecipitationKind.None,
                severity: WeatherSeverity.Calm),
            CurrentState: OutboxTestSupport.CreateWeatherState(
                startedAt: changedAt,
                expectedUntil: changedAt.Add(TimeSpan.FromHours(2)),
                type: WeatherType.Rain,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Moderate),
            AtSimTime: changedAt);

        await writer.AddWeatherEventsAsync([createdEvent, changedEvent], CancellationToken.None);
        await dbContext.SaveChangesAsync();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
           .AsNoTracking()
           .OrderBy(x => x.Type)
           .ToListAsync();

        Assert.Equal(2, messages.Count);

        OutboxMessage createdMessage = Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityWeatherCreatedV1);
        CityWeatherCreatedV1 createdPayload = OutboxTestSupport.DeserializePayload<CityWeatherCreatedV1>(createdMessage);
        Assert.Equal(occurredOnUtc.UtcDateTime, createdMessage.OccurredOnUtc);
        Assert.Equal(cityId.Value, createdPayload.CityId);
        Assert.Equal(createdEvent.AtSimTime.ValueUtc, createdPayload.AtSimTimeUtc);
        Assert.Equal(occurredOnUtc.UtcDateTime, createdPayload.OccurredOnUtc);
        Assert.Equal(createdEvent.ClimateProfile.ClimateZone.ToString(), createdPayload.ClimateProfile.ClimateZone);
        Assert.Equal(createdEvent.ClimateProfile.Volatility.Value, createdPayload.ClimateProfile.Volatility);
        Assert.Equal(createdEvent.InitialState.Type.ToString(), createdPayload.InitialState.Type);
        Assert.Equal(createdEvent.InitialState.Severity.ToString(), createdPayload.InitialState.Severity);
        Assert.Equal(createdEvent.InitialState.PrecipitationKind.ToString(), createdPayload.InitialState.PrecipitationKind);
        Assert.Equal(createdEvent.InitialState.ExpectedUntil.ValueUtc, createdPayload.InitialState.ExpectedUntilUtc);

        OutboxMessage changedMessage = Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityWeatherChangedV1);
        CityWeatherChangedV1 changedPayload = OutboxTestSupport.DeserializePayload<CityWeatherChangedV1>(changedMessage);
        Assert.Equal(occurredOnUtc.UtcDateTime, changedMessage.OccurredOnUtc);
        Assert.Equal(cityId.Value, changedPayload.CityId);
        Assert.Equal(changedEvent.AtSimTime.ValueUtc, changedPayload.AtSimTimeUtc);
        Assert.Equal(occurredOnUtc.UtcDateTime, changedPayload.OccurredOnUtc);
        Assert.Equal(changedEvent.PreviousState.Type.ToString(), changedPayload.PreviousState.Type);
        Assert.Equal(changedEvent.PreviousState.Severity.ToString(), changedPayload.PreviousState.Severity);
        Assert.Equal(changedEvent.CurrentState.Type.ToString(), changedPayload.CurrentState.Type);
        Assert.Equal(changedEvent.CurrentState.Severity.ToString(), changedPayload.CurrentState.Severity);
        Assert.Equal(changedEvent.CurrentState.PrecipitationKind.ToString(), changedPayload.CurrentState.PrecipitationKind);
        Assert.Equal(changedEvent.CurrentState.Temperature.Value, changedPayload.CurrentState.TemperatureC);
    }

    [Fact]
    public async Task AddWeatherEventsAsync_WhenOverrideAndClimateEventsAreAdded_WritesMatchingMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddWeatherEventsAsync_WhenOverrideAndClimateEventsAreAdded_WritesMatchingMessages));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(65);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var cityId = new CityId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        SimTime startsAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(5));
        SimTime endsAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(7));
        WeatherState forcedState = OutboxTestSupport.CreateWeatherState(
            startedAt: startsAt,
            expectedUntil: endsAt,
            type: WeatherType.Storm,
            precipitationKind: PrecipitationKind.Rain,
            severity: WeatherSeverity.Severe);
        WeatherClimateProfile previousProfile = OutboxTestSupport.CreateClimateProfile();
        WeatherClimateProfile currentProfile = WeatherClimateProfile.Create(
            climateZone: ClimateZone.Arid,
            temperatureProfile: previousProfile.TemperatureProfile,
            precipitationProfile: previousProfile.PrecipitationProfile,
            windProfile: previousProfile.WindProfile,
            volatility: WeatherVolatility.From(0.4m),
            extremeWeatherProfile: ExtremeWeatherProfile.Create(
                maxOverallSeverity: WeatherSeverity.Extreme,
                supportsThunderstorms: false,
                supportsSnowstorms: false,
                supportsFog: true,
                supportsHeatwaves: true));
        var startedEvent = new WeatherOverrideStartedDomainEvent(
            CityId: cityId,
            ForcedState: forcedState,
            Source: WeatherOverrideSource.System,
            StartsAt: startsAt,
            EndsAt: endsAt,
            Reason: "storm-front");
        var cancelledEvent = new WeatherOverrideCancelledDomainEvent(
            CityId: cityId,
            ForcedState: forcedState,
            Source: WeatherOverrideSource.Debug,
            CancelledAt: SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(6)));
        var expiredEvent = new WeatherOverrideExpiredDomainEvent(
            CityId: cityId,
            ForcedState: forcedState,
            Source: WeatherOverrideSource.Scenario,
            ExpiredAt: endsAt);
        var profileChangedEvent = new ClimateProfileChangedDomainEvent(
            CityId: cityId,
            PreviousProfile: previousProfile,
            CurrentProfile: currentProfile,
            AtSimTime: SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(8)));

        await writer.AddWeatherEventsAsync(
            [startedEvent, cancelledEvent, expiredEvent, profileChangedEvent],
            CancellationToken.None);
        await dbContext.SaveChangesAsync();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
           .AsNoTracking()
           .OrderBy(x => x.Type)
           .ToListAsync();

        Assert.Equal(4, messages.Count);

        WeatherOverrideStartedV1 startedPayload = OutboxTestSupport.DeserializePayload<WeatherOverrideStartedV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.WeatherOverrideStartedV1));
        Assert.Equal(cityId.Value, startedPayload.CityId);
        Assert.Equal(occurredOnUtc.UtcDateTime, startedPayload.OccurredOnUtc);
        Assert.Equal(forcedState.Type.ToString(), startedPayload.ForcedState.Type);
        Assert.Equal(WeatherOverrideSource.System.ToString(), startedPayload.Source);
        Assert.Equal(startsAt.ValueUtc, startedPayload.StartsAtUtc);
        Assert.Equal(endsAt.ValueUtc, startedPayload.EndsAtUtc);
        Assert.Equal("storm-front", startedPayload.Reason);

        WeatherOverrideCancelledV1 cancelledPayload = OutboxTestSupport.DeserializePayload<WeatherOverrideCancelledV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.WeatherOverrideCancelledV1));
        Assert.Equal(cityId.Value, cancelledPayload.CityId);
        Assert.Equal(occurredOnUtc.UtcDateTime, cancelledPayload.OccurredOnUtc);
        Assert.Equal(WeatherOverrideSource.Debug.ToString(), cancelledPayload.Source);
        Assert.Equal(cancelledEvent.CancelledAt.ValueUtc, cancelledPayload.CancelledAtUtc);

        WeatherOverrideExpiredV1 expiredPayload = OutboxTestSupport.DeserializePayload<WeatherOverrideExpiredV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.WeatherOverrideExpiredV1));
        Assert.Equal(cityId.Value, expiredPayload.CityId);
        Assert.Equal(occurredOnUtc.UtcDateTime, expiredPayload.OccurredOnUtc);
        Assert.Equal(WeatherOverrideSource.Scenario.ToString(), expiredPayload.Source);
        Assert.Equal(expiredEvent.ExpiredAt.ValueUtc, expiredPayload.ExpiredAtUtc);

        ClimateProfileChangedV1 profileChangedPayload = OutboxTestSupport.DeserializePayload<ClimateProfileChangedV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.ClimateProfileChangedV1));
        Assert.Equal(cityId.Value, profileChangedPayload.CityId);
        Assert.Equal(occurredOnUtc.UtcDateTime, profileChangedPayload.OccurredOnUtc);
        Assert.Equal(profileChangedEvent.AtSimTime.ValueUtc, profileChangedPayload.AtSimTimeUtc);
        Assert.Equal(previousProfile.ClimateZone.ToString(), profileChangedPayload.PreviousProfile.ClimateZone);
        Assert.Equal(previousProfile.Volatility.Value, profileChangedPayload.PreviousProfile.Volatility);
        Assert.Equal(currentProfile.ClimateZone.ToString(), profileChangedPayload.CurrentProfile.ClimateZone);
        Assert.Equal(currentProfile.Volatility.Value, profileChangedPayload.CurrentProfile.Volatility);
        Assert.Equal(currentProfile.ExtremeWeatherProfile.SupportsHeatwaves, profileChangedPayload.CurrentProfile.SupportsHeatwaves);
    }

    [Fact]
    public async Task AddWeatherEventsAsync_WhenWeatherDomainEventIsUnsupported_ThrowsInvalidOperationException()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddWeatherEventsAsync_WhenWeatherDomainEventIsUnsupported_ThrowsInvalidOperationException));
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => writer.AddWeatherEventsAsync([new UnsupportedWeatherDomainEvent()], CancellationToken.None));

        Assert.Contains("Unsupported weather domain event type", exception.Message);
        Assert.Empty(dbContext.OutboxMessages.Local);
    }

    private sealed record UnsupportedWeatherDomainEvent : DomainEventBase;
}
