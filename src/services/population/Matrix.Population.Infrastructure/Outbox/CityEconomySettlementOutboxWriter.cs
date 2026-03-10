using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Infrastructure.Persistence;

namespace Matrix.Population.Infrastructure.Outbox
{
    public sealed class CityEconomySettlementOutboxWriter(PopulationDbContext dbContext)
        : ICityEconomySettlementOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task AddCityDailySettlementAsync(
            CityEconomyDailySettlementV1 settlement,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.CityEconomyDailySettlementV1,
                    occurredOnUtc: settlement.OccurredAtUtc.UtcDateTime,
                    payload: settlement,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
