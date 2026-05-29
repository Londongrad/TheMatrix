using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public sealed class SimulationCoreOutboxWriter(
        SimulationCoreDbContext dbContext,
        TimeProvider timeProvider) : ISimulationCoreOutboxWriter
    {
        public Task AddSimulationEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents.Count == 0)
                return Task.CompletedTask;

            DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;

            foreach (IDomainEvent domainEvent in domainEvents)
            {
                OutboxMessage? message = domainEvent switch
                {
                    SimulationCreatedDomainEvent created => OutboxMessage.Create(
                        type: IntegrationEventTypes.SimulationCreatedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationCreatedV1(
                            SimulationId: created.SimulationId.Value,
                            HostId: created.HostId.Value,
                            ScenarioKey: created.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: created.RuntimeKey.HostTypeKey.Value,
                            Seed: created.Seed.Value,
                            RunId: created.RunId,
                            ModelVersion: created.ModelVersion.Value,
                            ProvisioningCorrelationId: created.ProvisioningCorrelationId,
                            State: created.State.ToString(),
                            CreatedAtUtc: created.CreatedAtUtc)),
                    SimulationArchivedDomainEvent archived => OutboxMessage.Create(
                        type: IntegrationEventTypes.SimulationArchivedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationArchivedV1(
                            SimulationId: archived.SimulationId.Value,
                            HostId: archived.HostId.Value,
                            ScenarioKey: archived.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: archived.RuntimeKey.HostTypeKey.Value,
                            ArchivedAtUtc: archived.ArchivedAtUtc)),
                    SimulationDeletedDomainEvent deleted => OutboxMessage.Create(
                        type: IntegrationEventTypes.SimulationDeletedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new SimulationDeletedV1(
                            SimulationId: deleted.SimulationId.Value,
                            HostId: deleted.HostId.Value,
                            ScenarioKey: deleted.RuntimeKey.ScenarioKey.Value,
                            HostTypeKey: deleted.RuntimeKey.HostTypeKey.Value,
                            DeletedAtUtc: deleted.DeletedAtUtc)),
                    _ => null
                };

                if (message is not null)
                    dbContext.OutboxMessages.Add(message);
            }

            return Task.CompletedTask;
        }

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
                            type: IntegrationEventTypes.CityCreatedV1,
                            occurredOnUtc: occurredOnUtc.UtcDateTime,
                            payload: new CityCreatedV1(
                                CityId: created.CityId.Value,
                                Name: created.Name.Value,
                                SimulationKind: created.SimulationKind.ToString(),
                                CreatedAtUtc: created.CreatedAtUtc,
                                DevelopmentLevel: created.GenerationProfile.DevelopmentLevel.ToString(),
                                EconomyProfile: created.GenerationProfile.EconomyProfile.ToString(),
                                RunId: created.RunId,
                                SimulationSeed: created.GenerationSeed.Value,
                                ScenarioModelSetVersion: created.ScenarioModelSetVersion.Value)));

                    dbContext.OutboxMessages.Add(
                        OutboxMessage.Create(
                            type: IntegrationEventTypes.CityEnvironmentChangedV1,
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
                    CityArchivedDomainEvent archived => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityArchivedV1,
                        occurredOnUtc: occurredOnUtc.UtcDateTime,
                        payload: new CityArchivedV1(
                            CityId: archived.CityId.Value,
                            ArchivedAtUtc: archived.ArchivedAtUtc)),
                    CityDeletedDomainEvent deleted => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityDeletedV1,
                        occurredOnUtc: occurredOnUtc.UtcDateTime,
                        payload: new CityDeletedV1(
                            CityId: deleted.CityId.Value,
                            DeletedAtUtc: deleted.DeletedAtUtc)),
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

        public Task AddSimulationTickPhaseReachedAsync(
            SimulationHost host,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            SimulationPhaseKey phaseKey,
            CancellationToken cancellationToken)
        {
            DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
            string correlationId =
                $"simulation:{host.SimulationId.Value:N}:host:{host.HostId.Value:N}:tick:{tickId.Value}";
            string causationId = $"{correlationId}:phase:{phaseKey.Value}";

            var integrationEvent = new SimulationTickPhaseReachedV1(
                SimulationId: host.SimulationId.Value,
                HostId: host.HostId.Value,
                ScenarioKey: host.RuntimeKey.ScenarioKey.Value,
                HostTypeKey: host.RuntimeKey.HostTypeKey.Value,
                PhaseKey: phaseKey.Value,
                FromSimTimeUtc: from.ValueUtc,
                ToSimTimeUtc: to.ValueUtc,
                TickId: tickId.Value,
                SpeedMultiplier: speed.Multiplier,
                ModelVersion: 1,
                CausationId: causationId,
                CorrelationId: correlationId,
                OccurredOnUtc: occurredOnUtc);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: IntegrationEventTypes.SimulationTickPhaseReachedV1,
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

            DateTime occurredOnUtc = timeProvider.GetUtcNow()
               .UtcDateTime;

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
