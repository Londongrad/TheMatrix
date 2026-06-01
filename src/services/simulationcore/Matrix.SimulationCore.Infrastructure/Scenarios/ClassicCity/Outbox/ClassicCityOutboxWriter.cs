using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Infrastructure.Persistence;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxWriter(
        SimulationCoreDbContext dbContext,
        TimeProvider timeProvider) : IClassicCityOutboxWriter
    {
        public Task AddCityEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTimeOffset occurredOnUtc = timeProvider.GetUtcNow();

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                if (domainEvent is CityCreatedDomainEvent created)
                {
                    dbContext.OutboxMessages.Add(
                        OutboxMessage.Create(
                            type: SimulationCoreEventTypes.ClassicCityCreatedV1,
                            occurredOnUtc: occurredOnUtc.UtcDateTime,
                            payload: new ClassicCityCreatedV1(
                                SimulationId: created.CityId.Value,
                                HostId: created.CityId.Value,
                                ScenarioKey: ClassicCityRuntime.ScenarioKey.Value,
                                HostTypeKey: ClassicCityRuntime.HostTypeKey.Value,
                                Name: created.Name.Value,
                                CreatedAtUtc: created.CreatedAtUtc,
                                DevelopmentLevel: created.GenerationProfile.DevelopmentLevel.ToString(),
                                EconomyProfile: created.GenerationProfile.EconomyProfile.ToString(),
                                RunId: created.RunId,
                                SimulationSeed: created.GenerationSeed.Value,
                                ScenarioModelSetVersion: created.ScenarioModelSetVersion.Value)));

                    dbContext.OutboxMessages.Add(
                        OutboxMessage.Create(
                            type: SimulationCoreEventTypes.CityEnvironmentChangedV1,
                            occurredOnUtc: occurredOnUtc.UtcDateTime,
                            payload: new CityEnvironmentChangedV1(
                                CityId: created.CityId.Value,
                                PreviousEnvironment: null,
                                CurrentEnvironment: ToCityEnvironment(created.Environment),
                                OccurredOnUtc: occurredOnUtc)));

                    continue;
                }

                OutboxMessage? message = domainEvent switch
                {
                    CityEnvironmentChangedDomainEvent changed => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.CityEnvironmentChangedV1,
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

        public Task AddWeatherEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTime occurredOnUtc = timeProvider.GetUtcNow()
               .UtcDateTime;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                OutboxMessage message = domainEvent switch
                {
                    CityWeatherCreatedDomainEvent created => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.CityWeatherCreatedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new CityWeatherCreatedV1(
                            CityId: created.CityId.Value,
                            ClimateProfile: ToWeatherClimateProfile(created.ClimateProfile),
                            InitialState: ToWeatherState(created.InitialState),
                            AtSimTimeUtc: created.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    CityWeatherChangedDomainEvent changed => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.CityWeatherChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new CityWeatherChangedV1(
                            CityId: changed.CityId.Value,
                            PreviousState: ToWeatherState(changed.PreviousState),
                            CurrentState: ToWeatherState(changed.CurrentState),
                            AtSimTimeUtc: changed.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideStartedDomainEvent started => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.WeatherOverrideStartedV1,
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
                        type: SimulationCoreEventTypes.WeatherOverrideCancelledV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideCancelledV1(
                            CityId: cancelled.CityId.Value,
                            ForcedState: ToWeatherState(cancelled.ForcedState),
                            Source: cancelled.Source.ToString(),
                            CancelledAtUtc: cancelled.CancelledAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideExpiredDomainEvent expired => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.WeatherOverrideExpiredV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideExpiredV1(
                            CityId: expired.CityId.Value,
                            ForcedState: ToWeatherState(expired.ForcedState),
                            Source: expired.Source.ToString(),
                            ExpiredAtUtc: expired.ExpiredAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    ClimateProfileChangedDomainEvent profileChanged => OutboxMessage.Create(
                        type: SimulationCoreEventTypes.ClimateProfileChangedV1,
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
