using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Time;
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
    }
}
