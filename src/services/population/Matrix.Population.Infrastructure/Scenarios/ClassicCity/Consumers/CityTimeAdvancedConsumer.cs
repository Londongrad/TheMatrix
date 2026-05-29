using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<SimulationTickPhaseReachedV1>
    {
        public Task Consume(ConsumeContext<SimulationTickPhaseReachedV1> context)
        {
            return ConsumeAsync(
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
                    ClassicCityTickPhaseKeys.PopulationReaction,
                    StringComparison.Ordinal))
                return;

            AdvanceCityPopulationResult result = await mediator.Send(
                request: new AdvanceCityPopulationCommand(
                    CityId: message.HostId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc,
                    TickId: message.TickId),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case AdvanceCityPopulationStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied city population progression for cityId={CityId}, tickId={TickId}, affectedPeople={AffectedPeople}.",
                        message.HostId,
                        message.TickId,
                        result.AffectedPeopleCount);
                    break;

                case AdvanceCityPopulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city population progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order city population progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city population progression for deleted cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.CityArchived:
                    logger.LogDebug(
                        message: "Skipped city population progression for archived cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;
            }
        }
    }
}
