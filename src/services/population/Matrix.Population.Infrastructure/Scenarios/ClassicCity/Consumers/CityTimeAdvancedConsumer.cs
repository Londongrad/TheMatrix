using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTickPhaseReachedV1>
    {
        public Task Consume(ConsumeContext<CityTickPhaseReachedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (message.TickContext.Phase != CityTickPhaseV1.PopulationReaction)
                return;

            AdvanceCityPopulationResult result = await mediator.Send(
                request: new AdvanceCityPopulationCommand(
                    CityId: message.CityId,
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
                        message.CityId,
                        message.TickId,
                        result.AffectedPeopleCount);
                    break;

                case AdvanceCityPopulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city population progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order city population progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city population progression for deleted cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.CityArchived:
                    logger.LogDebug(
                        message: "Skipped city population progression for archived cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
