using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Infrastructure.Outbox;
using Matrix.Resources.Infrastructure.Persistence;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class CityStockpileSnapshotOutboxWriter(ResourcesDbContext dbContext)
        : ICityStockpileSnapshotOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task AddClassicCityStockpileSnapshotAsync(
            ClassicCityStockpileSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: ResourcesOutboxEventTypes.ClassicCityStockpileSnapshotV1,
                    occurredOnUtc: snapshot.OccurredAtUtc.UtcDateTime,
                    payload: snapshot,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
