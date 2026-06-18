using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityOperationalBudgetPressureConsumer(
        IMediator mediator,
        ILogger<CityOperationalBudgetPressureConsumer> logger)
        : IConsumer<ClassicCityOperationalBudgetPressureSnapshotV1>
    {
        public Task Consume(ConsumeContext<ClassicCityOperationalBudgetPressureSnapshotV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityOperationalBudgetPressureSnapshotV1 message,
            CancellationToken cancellationToken)
        {
            SyncCityOperationalBudgetPressureResult result = await mediator.Send(
                request: new SyncCityOperationalBudgetPressureCommand(
                    CityId: message.CityId,
                    Balance: message.Balance,
                    MunicipalOperationsExpenses: message.MunicipalOperationsExpenses,
                    GeneralAvailableAmount: message.GeneralAvailableAmount,
                    OperationsAvailableAmount: message.OperationsAvailableAmount,
                    InfrastructureAvailableAmount: message.InfrastructureAvailableAmount,
                    HealthcareAvailableAmount: message.HealthcareAvailableAmount,
                    GeneralAuthorizationLevel: message.GeneralAuthorizationLevel,
                    OperationsAuthorizationLevel: message.OperationsAuthorizationLevel,
                    InfrastructureAuthorizationLevel: message.InfrastructureAuthorizationLevel,
                    HealthcareAuthorizationLevel: message.HealthcareAuthorizationLevel,
                    PressureIndex: message.PressureIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case SyncCityOperationalBudgetPressureStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city operational budget pressure for cityId={CityId}, effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}, pressure={Pressure}.",
                        message.CityId,
                        result.EffectiveTickId,
                        result.EffectiveAtUtc,
                        result.PressureIndex);
                    break;

                case SyncCityOperationalBudgetPressureStatus.Stale:
                    logger.LogWarning(
                        message:
                        "Skipped stale classic city operational budget pressure for cityId={CityId}, effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        message.EffectiveTickId,
                        message.EffectiveAtUtc);
                    break;

                case SyncCityOperationalBudgetPressureStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city operational budget pressure for cityId={CityId} because environmental state is not initialized yet.",
                        message.CityId);
                    break;

                case SyncCityOperationalBudgetPressureStatus.Concurrent:
                    logger.LogDebug(
                        message:
                        "Skipped classic city operational budget pressure for cityId={CityId} after a concurrent update won the persistence race. Current effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        result.EffectiveTickId,
                        result.EffectiveAtUtc);
                    break;
            }
        }
    }
}
