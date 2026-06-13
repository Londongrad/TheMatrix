using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;

namespace Matrix.Economy.Infrastructure.Outbox
{
    public sealed class CityPopulationSignalOutboxWriter(EconomyDbContext dbContext)
        : ICityPopulationSignalPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task PublishClassicCityCostOfLivingSnapshotAsync(
            ClassicCityCostOfLivingSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            return AddAsync(
                    type: ClassicCityOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1,
                payload: snapshot,
                occurredAtUtc: snapshot.OccurredAtUtc);
        }

        public Task PublishClassicCityServiceQualitySnapshotAsync(
            ClassicCityServiceQualitySnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            return AddAsync(
                    type: ClassicCityOutboxEventTypes.ClassicCityServiceQualitySnapshotV1,
                payload: snapshot,
                occurredAtUtc: snapshot.OccurredAtUtc);
        }

        public Task PublishClassicCityEmployerFinancialStressBatchAsync(
            ClassicCityEmployerFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            return AddAsync(
                    type: ClassicCityOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1,
                payload: batch,
                occurredAtUtc: batch.OccurredAtUtc);
        }

        public Task PublishClassicCityHouseholdFinancialStressBatchAsync(
            ClassicCityHouseholdFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            return AddAsync(
                    type: ClassicCityOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1,
                payload: batch,
                occurredAtUtc: batch.OccurredAtUtc);
        }

        private Task AddAsync<TPayload>(
            string type,
            TPayload payload,
            DateTimeOffset occurredAtUtc)
            where TPayload : notnull
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: type,
                    occurredOnUtc: occurredAtUtc.UtcDateTime,
                    payload: payload,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
