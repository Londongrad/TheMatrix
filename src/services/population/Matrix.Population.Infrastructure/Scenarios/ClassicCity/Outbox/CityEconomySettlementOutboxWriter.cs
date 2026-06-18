using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Persistence;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox
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

        public Task AddClassicCityHouseholdAccountSyncBatchAsync(
            ClassicCityHouseholdAccountSyncBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }

        public Task AddClassicCityWorkplaceBusinessSyncBatchAsync(
            ClassicCityWorkplaceBusinessSyncBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }

        public Task AddClassicCityWorkplacePayrollSettlementBatchAsync(
            ClassicCityWorkplacePayrollSettlementBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }

        public Task AddClassicCityHouseholdCashflowSettlementBatchAsync(
            ClassicCityHouseholdCashflowSettlementBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1,
                    occurredOnUtc: batch.OccurredAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
