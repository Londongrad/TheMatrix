using System.Text.Json;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Infrastructure.Persistence;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class CityOperationalExpenseOutboxWriter(SimulationSystemsDbContext dbContext)
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
