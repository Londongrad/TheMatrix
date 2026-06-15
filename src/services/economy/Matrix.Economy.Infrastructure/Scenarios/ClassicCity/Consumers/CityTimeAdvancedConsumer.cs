using MassTransit;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ICityEconomyDeletionRepository deletionRepository,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<SimulationTickPhaseReachedV1>
    {
        public async Task Consume(ConsumeContext<SimulationTickPhaseReachedV1> context)
        {
            await ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey) ||
                !string.Equals(
                    message.PhaseKey,
                    ClassicCityTickPhaseKeys.BudgetSettlement,
                    StringComparison.Ordinal))
                return;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.HostId,
                    cancellationToken: cancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped city economy progression for deleted cityId={CityId}, tickId={TickId}.",
                    message.HostId,
                    message.TickId);
                return;
            }

            AdvanceCityEconomySimulationResult result = await mediator.Send(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: message.HostId,
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
                        message.HostId,
                        message.TickId,
                        result.ProcessedDays,
                        result.ChargedObligations,
                        result.RemittedBusinesses,
                        result.MunicipalProviderPayments);
                    break;

                case AdvanceCityEconomySimulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city economy progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityEconomySimulationStatus.OutOfOrder:
                    logger.LogDebug(
                        message: "Skipped out-of-order city economy progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;
            }
        }
    }
}
