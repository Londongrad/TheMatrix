using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;

namespace Matrix.Economy.Infrastructure.Messaging
{
    public sealed class MassTransitCityOperationalBudgetSignalPublisher(IPublishEndpoint publishEndpoint)
        : ICityOperationalBudgetSignalPublisher
    {
        public Task PublishClassicCityOperationalBudgetPressureSnapshotAsync(
            CityOperationalBudgetPressureDto snapshot,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            return publishEndpoint.Publish(
                message: new ClassicCityOperationalBudgetPressureSnapshotV1(
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
                    EffectiveAtUtc: effectiveAtUtc,
                    OccurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
        }
    }
}
