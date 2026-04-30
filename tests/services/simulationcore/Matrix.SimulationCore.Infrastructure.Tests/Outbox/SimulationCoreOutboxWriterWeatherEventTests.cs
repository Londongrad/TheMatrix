using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
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
}
