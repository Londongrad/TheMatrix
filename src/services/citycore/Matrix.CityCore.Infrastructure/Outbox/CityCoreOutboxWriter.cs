using Matrix.BuildingBlocks.Domain.Events;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Weather;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents;
using Matrix.CityCore.Infrastructure.Persistence;

namespace Matrix.CityCore.Infrastructure.Outbox
{
    public sealed class CityCoreOutboxWriter(CityCoreDbContext dbContext) : ICityCoreOutboxWriter
    {
        public Task AddCityTimeAdvancedAsync(
            CityId cityId,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CancellationToken cancellationToken)
        {
            DateTime occurredOnUtc = DateTime.UtcNow;

            var integrationEvent = new CityTimeAdvancedIntegrationEvent(
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
                        payload: new CityWeatherCreatedIntegrationEvent(
                            CityId: created.CityId.Value,
                            ClimateProfile: WeatherClimateProfileIntegrationData.FromDomain(created.ClimateProfile),
                            InitialState: WeatherStateIntegrationData.FromDomain(created.InitialState),
                            AtSimTimeUtc: created.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    CityWeatherChangedDomainEvent changed => OutboxMessage.Create(
                        type: IntegrationEventTypes.CityWeatherChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new CityWeatherChangedIntegrationEvent(
                            CityId: changed.CityId.Value,
                            PreviousState: WeatherStateIntegrationData.FromDomain(changed.PreviousState),
                            CurrentState: WeatherStateIntegrationData.FromDomain(changed.CurrentState),
                            AtSimTimeUtc: changed.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideStartedDomainEvent started => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideStartedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideStartedIntegrationEvent(
                            CityId: started.CityId.Value,
                            ForcedState: WeatherStateIntegrationData.FromDomain(started.ForcedState),
                            Source: started.Source.ToString(),
                            StartsAtUtc: started.StartsAt.ValueUtc,
                            EndsAtUtc: started.EndsAt.ValueUtc,
                            Reason: started.Reason,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideCancelledDomainEvent cancelled => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideCancelledV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideCancelledIntegrationEvent(
                            CityId: cancelled.CityId.Value,
                            ForcedState: WeatherStateIntegrationData.FromDomain(cancelled.ForcedState),
                            Source: cancelled.Source.ToString(),
                            CancelledAtUtc: cancelled.CancelledAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    WeatherOverrideExpiredDomainEvent expired => OutboxMessage.Create(
                        type: IntegrationEventTypes.WeatherOverrideExpiredV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new WeatherOverrideExpiredIntegrationEvent(
                            CityId: expired.CityId.Value,
                            ForcedState: WeatherStateIntegrationData.FromDomain(expired.ForcedState),
                            Source: expired.Source.ToString(),
                            ExpiredAtUtc: expired.ExpiredAt.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    ClimateProfileChangedDomainEvent profileChanged => OutboxMessage.Create(
                        type: IntegrationEventTypes.ClimateProfileChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: new ClimateProfileChangedIntegrationEvent(
                            CityId: profileChanged.CityId.Value,
                            PreviousProfile: WeatherClimateProfileIntegrationData.FromDomain(
                                profileChanged.PreviousProfile),
                            CurrentProfile: WeatherClimateProfileIntegrationData.FromDomain(
                                profileChanged.CurrentProfile),
                            AtSimTimeUtc: profileChanged.AtSimTime.ValueUtc,
                            OccurredOnUtc: occurredOnUtc)),
                    _ => throw new InvalidOperationException(
                        $"Unsupported weather domain event type '{domainEvent.GetType().Name}'.")
                };

                dbContext.OutboxMessages.Add(message);
            }

            return Task.CompletedTask;
        }
    }
}
