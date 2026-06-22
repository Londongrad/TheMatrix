using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Infrastructure.Persistence;

namespace Matrix.Population.Infrastructure.Outbox
{
    public sealed class PopulationResidentFactsOutboxWriter(PopulationDbContext dbContext)
        : IPopulationResidentFactsOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task AddResidentFactsBatchAsync(
            PopulationResidentFactsBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: PopulationOutboxEventTypes.PopulationResidentFactsBatchV1,
                    occurredOnUtc: batch.SynchronizedAtUtc.UtcDateTime,
                    payload: batch,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
