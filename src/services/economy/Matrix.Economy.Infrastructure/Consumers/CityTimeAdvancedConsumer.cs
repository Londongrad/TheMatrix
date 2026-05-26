using MassTransit;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ICityEconomyDeletionRepository deletionRepository,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTickPhaseReachedV1>
    {
        public async Task Consume(ConsumeContext<CityTickPhaseReachedV1> context)
        {
            await ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (message.TickContext.Phase != CityTickPhaseV1.BudgetSettlement)
                return;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: cancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped city economy progression for deleted cityId={CityId}, tickId={TickId}.",
                    message.CityId,
                    message.TickId);
                return;
            }

            AdvanceCityEconomySimulationResult result = await mediator.Send(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: message.CityId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc,
                    TickId: message.TickId),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case AdvanceCityEconomySimulationStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied city economy progression for cityId={CityId}, tickId={TickId}, processedDays={ProcessedDays}, chargedObligations={ChargedObligations}, remittedBusinesses={RemittedBusinesses}, municipalPayments={MunicipalPayments}.",
                        message.CityId,
                        message.TickId,
                        result.ProcessedDays,
                        result.ChargedObligations,
                        result.RemittedBusinesses,
                        result.MunicipalProviderPayments);
                    break;

                case AdvanceCityEconomySimulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city economy progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityEconomySimulationStatus.OutOfOrder:
                    logger.LogDebug(
                        message: "Skipped out-of-order city economy progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
