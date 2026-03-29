using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityOperationalBudgetPressureConsumer(
        IMediator mediator,
        ILogger<CityOperationalBudgetPressureConsumer> logger)
        : IConsumer<ClassicCityOperationalBudgetPressureSnapshotV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityOperationalBudgetPressureSnapshotV1> context)
        {
            ClassicCityOperationalBudgetPressureSnapshotV1 message = context.Message;

            SyncCityOperationalBudgetPressureResult result = await mediator.Send(
                request: new SyncCityOperationalBudgetPressureCommand(
                    CityId: message.CityId,
                    Balance: message.Balance,
                    MunicipalOperationsExpenses: message.MunicipalOperationsExpenses,
                    PressureIndex: message.PressureIndex,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SyncCityOperationalBudgetPressureStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city operational budget pressure for cityId={CityId}, effectiveAtUtc={EffectiveAtUtc}, pressure={Pressure}.",
                        message.CityId,
                        result.EffectiveAtUtc,
                        result.PressureIndex);
                    break;

                case SyncCityOperationalBudgetPressureStatus.Stale:
                    logger.LogWarning(
                        message:
                        "Skipped stale classic city operational budget pressure for cityId={CityId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        message.EffectiveAtUtc);
                    break;

                case SyncCityOperationalBudgetPressureStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city operational budget pressure for cityId={CityId} because environmental state is not initialized yet.",
                        message.CityId);
                    break;
            }
        }
    }
}
