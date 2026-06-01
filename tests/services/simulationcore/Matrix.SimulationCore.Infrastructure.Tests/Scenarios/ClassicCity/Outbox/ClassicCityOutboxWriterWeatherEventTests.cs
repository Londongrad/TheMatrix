using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.SimulationCore.Infrastructure.Tests.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxWriterWeatherEventTests
    {
        [Fact]
        public async Task AddWeatherEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddWeatherEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages));
            var writer = new ClassicCityOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

            await writer.AddWeatherEventsAsync(
                domainEvents: [],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Empty(
                await dbContext.OutboxMessages.AsNoTracking()
                   .ToListAsync());
        }

        [Fact]
        public async Task AddWeatherEventsAsync_WhenCreatedAndChangedEventsAreAdded_WritesMatchingMessages()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddWeatherEventsAsync_WhenCreatedAndChangedEventsAreAdded_WritesMatchingMessages));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(55);
            var writer = new ClassicCityOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            var cityId = new CityId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var createdAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(2));
            var changedAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(3));
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

            await writer.AddWeatherEventsAsync(
                domainEvents:
                [
                    createdEvent,
                    changedEvent
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            List<OutboxMessage> messages = await dbContext.OutboxMessages
               .AsNoTracking()
               .OrderBy(x => x.Type)
               .ToListAsync();

            Assert.Equal(
                expected: 2,
                actual: messages.Count);

            OutboxMessage createdMessage = Assert.Single(
                collection: messages,
                predicate: x => x.Type == SimulationCoreEventTypes.CityWeatherCreatedV1);
            CityWeatherCreatedV1 createdPayload =
                OutboxTestSupport.DeserializePayload<CityWeatherCreatedV1>(createdMessage);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: createdMessage.OccurredOnUtc);
            Assert.Equal(
                expected: cityId.Value,
                actual: createdPayload.CityId);
            Assert.Equal(
                expected: createdEvent.AtSimTime.ValueUtc,
                actual: createdPayload.AtSimTimeUtc);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: createdPayload.OccurredOnUtc);
            Assert.Equal(
                expected: createdEvent.ClimateProfile.ClimateZone.ToString(),
                actual: createdPayload.ClimateProfile.ClimateZone);
            Assert.Equal(
                expected: createdEvent.ClimateProfile.Volatility.Value,
                actual: createdPayload.ClimateProfile.Volatility);
            Assert.Equal(
                expected: createdEvent.InitialState.Type.ToString(),
                actual: createdPayload.InitialState.Type);
            Assert.Equal(
                expected: createdEvent.InitialState.Severity.ToString(),
                actual: createdPayload.InitialState.Severity);
            Assert.Equal(
                expected: createdEvent.InitialState.PrecipitationKind.ToString(),
                actual: createdPayload.InitialState.PrecipitationKind);
            Assert.Equal(
                expected: createdEvent.InitialState.ExpectedUntil.ValueUtc,
                actual: createdPayload.InitialState.ExpectedUntilUtc);

            OutboxMessage changedMessage = Assert.Single(
                collection: messages,
                predicate: x => x.Type == SimulationCoreEventTypes.CityWeatherChangedV1);
            CityWeatherChangedV1 changedPayload =
                OutboxTestSupport.DeserializePayload<CityWeatherChangedV1>(changedMessage);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: changedMessage.OccurredOnUtc);
            Assert.Equal(
                expected: cityId.Value,
                actual: changedPayload.CityId);
            Assert.Equal(
                expected: changedEvent.AtSimTime.ValueUtc,
                actual: changedPayload.AtSimTimeUtc);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: changedPayload.OccurredOnUtc);
            Assert.Equal(
                expected: changedEvent.PreviousState.Type.ToString(),
                actual: changedPayload.PreviousState.Type);
            Assert.Equal(
                expected: changedEvent.PreviousState.Severity.ToString(),
                actual: changedPayload.PreviousState.Severity);
            Assert.Equal(
                expected: changedEvent.CurrentState.Type.ToString(),
                actual: changedPayload.CurrentState.Type);
            Assert.Equal(
                expected: changedEvent.CurrentState.Severity.ToString(),
                actual: changedPayload.CurrentState.Severity);
            Assert.Equal(
                expected: changedEvent.CurrentState.PrecipitationKind.ToString(),
                actual: changedPayload.CurrentState.PrecipitationKind);
            Assert.Equal(
                expected: changedEvent.CurrentState.Temperature.Value,
                actual: changedPayload.CurrentState.TemperatureC);
        }

        [Fact]
        public async Task AddWeatherEventsAsync_WhenOverrideAndClimateEventsAreAdded_WritesMatchingMessages()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddWeatherEventsAsync_WhenOverrideAndClimateEventsAreAdded_WritesMatchingMessages));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(65);
            var writer = new ClassicCityOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            var cityId = new CityId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            var startsAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(5));
            var endsAt = SimTime.FromUtc(OutboxTestSupport.BaseUtc.AddHours(7));
            WeatherState forcedState = OutboxTestSupport.CreateWeatherState(
                startedAt: startsAt,
                expectedUntil: endsAt,
                type: WeatherType.Storm,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Severe);
            WeatherClimateProfile previousProfile = OutboxTestSupport.CreateClimateProfile();
            var currentProfile = WeatherClimateProfile.Create(
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
                domainEvents:
                [
                    startedEvent,
                    cancelledEvent,
                    expiredEvent,
                    profileChangedEvent
                ],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            List<OutboxMessage> messages = await dbContext.OutboxMessages
               .AsNoTracking()
               .OrderBy(x => x.Type)
               .ToListAsync();

            Assert.Equal(
                expected: 4,
                actual: messages.Count);

            WeatherOverrideStartedV1 startedPayload = OutboxTestSupport.DeserializePayload<WeatherOverrideStartedV1>(
                Assert.Single(
                    collection: messages,
                    predicate: x => x.Type == SimulationCoreEventTypes.WeatherOverrideStartedV1));
            Assert.Equal(
                expected: cityId.Value,
                actual: startedPayload.CityId);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: startedPayload.OccurredOnUtc);
            Assert.Equal(
                expected: forcedState.Type.ToString(),
                actual: startedPayload.ForcedState.Type);
            Assert.Equal(
                expected: WeatherOverrideSource.System.ToString(),
                actual: startedPayload.Source);
            Assert.Equal(
                expected: startsAt.ValueUtc,
                actual: startedPayload.StartsAtUtc);
            Assert.Equal(
                expected: endsAt.ValueUtc,
                actual: startedPayload.EndsAtUtc);
            Assert.Equal(
                expected: "storm-front",
                actual: startedPayload.Reason);

            WeatherOverrideCancelledV1 cancelledPayload =
                OutboxTestSupport.DeserializePayload<WeatherOverrideCancelledV1>(
                    Assert.Single(
                        collection: messages,
                        predicate: x => x.Type == SimulationCoreEventTypes.WeatherOverrideCancelledV1));
            Assert.Equal(
                expected: cityId.Value,
                actual: cancelledPayload.CityId);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: cancelledPayload.OccurredOnUtc);
            Assert.Equal(
                expected: WeatherOverrideSource.Debug.ToString(),
                actual: cancelledPayload.Source);
            Assert.Equal(
                expected: cancelledEvent.CancelledAt.ValueUtc,
                actual: cancelledPayload.CancelledAtUtc);

            WeatherOverrideExpiredV1 expiredPayload = OutboxTestSupport.DeserializePayload<WeatherOverrideExpiredV1>(
                Assert.Single(
                    collection: messages,
                    predicate: x => x.Type == SimulationCoreEventTypes.WeatherOverrideExpiredV1));
            Assert.Equal(
                expected: cityId.Value,
                actual: expiredPayload.CityId);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: expiredPayload.OccurredOnUtc);
            Assert.Equal(
                expected: WeatherOverrideSource.Scenario.ToString(),
                actual: expiredPayload.Source);
            Assert.Equal(
                expected: expiredEvent.ExpiredAt.ValueUtc,
                actual: expiredPayload.ExpiredAtUtc);

            ClimateProfileChangedV1 profileChangedPayload =
                OutboxTestSupport.DeserializePayload<ClimateProfileChangedV1>(
                    Assert.Single(
                        collection: messages,
                        predicate: x => x.Type == SimulationCoreEventTypes.ClimateProfileChangedV1));
            Assert.Equal(
                expected: cityId.Value,
                actual: profileChangedPayload.CityId);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: profileChangedPayload.OccurredOnUtc);
            Assert.Equal(
                expected: profileChangedEvent.AtSimTime.ValueUtc,
                actual: profileChangedPayload.AtSimTimeUtc);
            Assert.Equal(
                expected: previousProfile.ClimateZone.ToString(),
                actual: profileChangedPayload.PreviousProfile.ClimateZone);
            Assert.Equal(
                expected: previousProfile.Volatility.Value,
                actual: profileChangedPayload.PreviousProfile.Volatility);
            Assert.Equal(
                expected: currentProfile.ClimateZone.ToString(),
                actual: profileChangedPayload.CurrentProfile.ClimateZone);
            Assert.Equal(
                expected: currentProfile.Volatility.Value,
                actual: profileChangedPayload.CurrentProfile.Volatility);
            Assert.Equal(
                expected: currentProfile.ExtremeWeatherProfile.SupportsHeatwaves,
                actual: profileChangedPayload.CurrentProfile.SupportsHeatwaves);
        }

        [Fact]
        public async Task AddWeatherEventsAsync_WhenWeatherDomainEventIsUnsupported_ThrowsInvalidOperationException()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddWeatherEventsAsync_WhenWeatherDomainEventIsUnsupported_ThrowsInvalidOperationException));
            var writer = new ClassicCityOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => writer.AddWeatherEventsAsync(
                    domainEvents: [new UnsupportedWeatherDomainEvent()],
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "Unsupported weather domain event type",
                actualString: exception.Message);
            Assert.Empty(dbContext.OutboxMessages.Local);
        }

        private sealed record UnsupportedWeatherDomainEvent : DomainEventBase;
    }
}
