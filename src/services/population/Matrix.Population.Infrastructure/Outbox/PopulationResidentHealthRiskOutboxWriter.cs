using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Infrastructure.Persistence;

namespace Matrix.Population.Infrastructure.Outbox
{
    public sealed class PopulationResidentHealthRiskOutboxWriter(PopulationDbContext dbContext)
        : IPopulationResidentHealthRiskOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task AddResidentHealthRiskBatchAsync(
            PopulationResidentHealthRiskBatchV2 batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV2,
                    occurredOnUtc: batch.ObservedAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
