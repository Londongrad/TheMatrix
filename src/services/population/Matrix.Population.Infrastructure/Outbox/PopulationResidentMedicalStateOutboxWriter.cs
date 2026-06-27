using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Infrastructure.Persistence;

namespace Matrix.Population.Infrastructure.Outbox
{
    public sealed class PopulationResidentMedicalStateOutboxWriter(PopulationDbContext dbContext)
        : IPopulationResidentMedicalStateOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task AddResidentMedicalStateBatchAsync(
            PopulationResidentMedicalStateBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.PopulationResidentMedicalStateBatchV1,
                    occurredOnUtc: batch.ObservedAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
