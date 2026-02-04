using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Contracts.Events;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Cities;
using Matrix.CityCore.Domain.Events.Weather;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;
using Matrix.CityCore.Infrastructure.Persistence;

namespace Matrix.CityCore.Infrastructure.Outbox
{
    public sealed class CityCoreOutboxWriter(CityCoreDbContext dbContext) : ICityCoreOutboxWriter
    {
        public Task AddCityEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTimeOffset occurredOnUtc = DateTimeOffset.UtcNow;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                OutboxMessage? message = domainEvent switch
                {
                    CityDeletedDomainEvent deleted => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityDeletedV1,
                        occurredOnUtc: occurredOnUtc.UtcDateTime,
                        payload: new CityDeletedV1(
                            CityId: deleted.CityId.Value,
                            DeletedAtUtc: deleted.DeletedAtUtc)),
                    CityCreatedDomainEvent created => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityEnvironmentChangedV1,
                        occurredOnUtc: occurredOnUtc.UtcDateTime,
                        payload: new CityEnvironmentChangedV1(
                            CityId: created.CityId.Value,
                            PreviousEnvironment: null,
                            CurrentEnvironment: ToCityEnvironment(created.Environment),
                            OccurredOnUtc: occurredOnUtc)),
                    CityEnvironmentChangedDomainEvent changed => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityEnvironmentChangedV1,
                        occurredOnUtc: occurredOnUtc.UtcDateTime,
                        payload: new CityEnvironmentChangedV1(
                            CityId: changed.CityId.Value,
                            PreviousEnvironment: ToCityEnvironment(changed.From),
                            CurrentEnvironment: ToCityEnvironment(changed.To),
                            OccurredOnUtc: occurredOnUtc)),
                    _ => null
                };

                if (message is not null)
                    dbContext.OutboxMessages.Add(message);
            }

            return Task.CompletedTask;
        }

        public Task AddCityTimeAdvancedAsync(
            CityId cityId,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CancellationToken cancellationToken)
        {
            DateTime occurredOnUtc = DateTime.UtcNow;

            var integrationEvent = new CityTimeAdvancedV1(
                CityId: cityId.Value,
                FromSimTimeUtc: from.ValueUtc,
                ToSimTimeUtc: to.ValueUtc,
                TickId: tickId.Value,
                SpeedMultiplier: speed.Multiplier,
                OccurredOnUtc: occurredOnUtc);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: IntegrationEventTypes.CityTimeAdvancedV1,
                    occurredOnUtc: occurredOnUtc,
                    payload: integrationEvent));

            return Task.CompletedTask;
        }

        public Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTime occurredOnUtc = DateTime.UtcNow;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                OutboxMessage message = domainEvent switch
                {
                    CityWeatherCreatedDomainEvent created => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityWeatherCreatedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new CityWeatherCreatedV1(
                            CityId: created.CityId.Value,
                            ClimateProfile: ToWeatherClimateProfile(created.ClimateProfile),
                            InitialState: ToWeatherState(created.InitialState),
                            AtSimTimeUtc: created.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    CityWeatherChangedDomainEvent changed => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityWeatherChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new CityWeatherChangedV1(
                            CityId: changed.CityId.Value,
                            PreviousState: ToWeatherState(changed.PreviousState),
                            CurrentState: ToWeatherState(changed.CurrentState),
                            AtSimTimeUtc: changed.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideStartedDomainEvent started => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideStartedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideStartedV1(
                            CityId: started.CityId.Value,
                            ForcedState: ToWeatherState(started.ForcedState),
                            Source: started.Source.ToString(),
                            StartsAtUtc: started.StartsAt.ValueUtc,
                            EndsAtUtc: started.EndsAt.ValueUtc,
                            Reason: started.Reason,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideCancelledDomainEvent cancelled => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideCancelledV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideCancelledV1(
                            CityId: cancelled.CityId.Value,
                            ForcedState: ToWeatherState(cancelled.ForcedState),
                            Source: cancelled.Source.ToString(),
                            CancelledAtUtc: cancelled.CancelledAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideExpiredDomainEvent expired => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideExpiredV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideExpiredV1(
                            CityId: expired.CityId.Value,
                            ForcedState: ToWeatherState(expired.ForcedState),
                            Source: expired.Source.ToString(),
                            ExpiredAtUtc: expired.ExpiredAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    ClimateProfileChangedDomainEvent profileChanged => OutboxMessage.Create(
                        type: IntegrationEventTypes.ClimateProfileChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new ClimateProfileChangedV1(
                            CityId: profileChanged.CityId.Value,
                            PreviousProfile: ToWeatherClimateProfile(profileChanged.PreviousProfile),
                            CurrentProfile: ToWeatherClimateProfile(profileChanged.CurrentProfile),
                            AtSimTimeUtc: profileChanged.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    _ => throw new InvalidOperationException(
                        $"Unsupported weather domain event type '{domainEvent.GetType().Name}'.")
                };

                dbContext.OutboxMessages.Add(message);
            }

            return Task.CompletedTask;
        }

        private static WeatherStateV1 ToWeatherState(WeatherState state)
        {
            return new WeatherStateV1(
                Type: state.Type.ToString(),
                Severity: state.Severity.ToString(),
                PrecipitationKind: state.PrecipitationKind.ToString(),
                TemperatureC: state.Temperature.Value,
                HumidityPercent: state.Humidity.Value,
                WindSpeedKph: state.WindSpeed.Value,
                CloudCoveragePercent: state.CloudCoverage.Value,
                PressureHpa: state.Pressure.Value,
                StartedAtUtc: state.StartedAt.ValueUtc,
                ExpectedUntilUtc: state.ExpectedUntil.ValueUtc);
        }

        private static WeatherClimateProfileV1 ToWeatherClimateProfile(WeatherClimateProfile profile)
        {
            return new WeatherClimateProfileV1(
                ClimateZone: profile.ClimateZone.ToString(),
                Volatility: profile.Volatility.Value,
                MaxOverallSeverity: profile.ExtremeWeatherProfile.MaxOverallSeverity.ToString(),
                SupportsThunderstorms: profile.ExtremeWeatherProfile.SupportsThunderstorms,
                SupportsSnowstorms: profile.ExtremeWeatherProfile.SupportsSnowstorms,
                SupportsFog: profile.ExtremeWeatherProfile.SupportsFog,
                SupportsHeatwaves: profile.ExtremeWeatherProfile.SupportsHeatwaves);
        }

        private static CityEnvironmentV1 ToCityEnvironment(CityEnvironment environment)
        {
            return new CityEnvironmentV1(
                ClimateZone: environment.ClimateZone.ToString(),
                Hemisphere: environment.Hemisphere.ToString(),
                UtcOffsetMinutes: environment.UtcOffset.TotalMinutes);
        }
    }
}
