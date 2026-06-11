using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox;

namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public sealed class CityPopulationLivingConditionsOutboxWriter(SimulationSystemsDbContext dbContext)
        : ICityPopulationLivingConditionsOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task AddClassicCityLivingConditionsSnapshotAsync(
            ClassicCityLivingConditionsSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: ClassicCityOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1,
                    occurredOnUtc: snapshot.OccurredAtUtc.UtcDateTime,
                    payload: snapshot,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
