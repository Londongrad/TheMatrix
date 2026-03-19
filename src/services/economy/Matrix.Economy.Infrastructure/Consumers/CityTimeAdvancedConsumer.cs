using MassTransit;
using Matrix.CityCore.Contracts.Events;
using Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTimeAdvancedV1>
    {
        public async Task Consume(ConsumeContext<CityTimeAdvancedV1> context)
        {
            CityTimeAdvancedV1 message = context.Message;

            AdvanceCityEconomySimulationResult result = await mediator.Send(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: message.CityId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc,
                    TickId: message.TickId),
                cancellationToken: context.CancellationToken);

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
                    logger.LogWarning(
                        message: "Skipped out-of-order city economy progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
