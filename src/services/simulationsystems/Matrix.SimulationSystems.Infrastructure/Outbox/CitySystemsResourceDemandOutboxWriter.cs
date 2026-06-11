using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox;

namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public sealed class CitySystemsResourceDemandOutboxWriter(SimulationSystemsDbContext dbContext)
        : ICitySystemsResourceDemandOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task AddClassicCitySystemsResourceDemandAsync(
            ClassicCitySystemsResourceDemandSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: ClassicCityOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1,
                    occurredOnUtc: snapshot.OccurredAtUtc.UtcDateTime,
                    payload: snapshot,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
