using System.Text.Json;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Infrastructure.Persistence;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class CityOperationalExpenseOutboxWriter(ResourcesDbContext dbContext)
        : ICityOperationalExpenseOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task AddClassicCityOperationalExpenseAsync(
            ClassicCityOperationalExpenseIncurredV1 expense,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1,
                    occurredOnUtc: expense.OccurredAtUtc.UtcDateTime,
                    payload: expense,
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
