using System.Text.Json;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class CityOperationalBudgetSignalOutboxWriter(EconomyDbContext dbContext)
        : ICityOperationalBudgetSignalPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task PublishClassicCityOperationalBudgetPressureSnapshotAsync(
            CityOperationalBudgetPressureDto snapshot,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: ClassicCityOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1,
                    occurredOnUtc: occurredAtUtc.UtcDateTime,
                    payload: new ClassicCityOperationalBudgetPressureSnapshotV1(
                        CityId: snapshot.CityId,
                        Balance: snapshot.Balance,
                        TotalCityExpenses: snapshot.TotalCityExpenses,
                        MunicipalOperationsExpenses: snapshot.MunicipalOperationsExpenses,
                        InfrastructureOperationsExpenses: snapshot.InfrastructureOperationsExpenses,
                        EmergencyOperationsExpenses: snapshot.EmergencyOperationsExpenses,
                        GeneralAvailableAmount: snapshot.GeneralAvailableAmount,
                        OperationsAvailableAmount: snapshot.OperationsAvailableAmount,
                        InfrastructureAvailableAmount: snapshot.InfrastructureAvailableAmount,
                        HealthcareAvailableAmount: snapshot.HealthcareAvailableAmount,
                        GeneralAuthorizationLevel: snapshot.GeneralAuthorizationLevel,
                        OperationsAuthorizationLevel: snapshot.OperationsAuthorizationLevel,
                        InfrastructureAuthorizationLevel: snapshot.InfrastructureAuthorizationLevel,
                        HealthcareAuthorizationLevel: snapshot.HealthcareAuthorizationLevel,
                        PressureIndex: snapshot.PressureIndex,
                        EffectiveTickId: snapshot.EffectiveTickId,
                        EffectiveAtUtc: effectiveAtUtc,
                        OccurredAtUtc: occurredAtUtc),
                    jsonOptions: JsonOptions));

            return Task.CompletedTask;
        }
    }
}
