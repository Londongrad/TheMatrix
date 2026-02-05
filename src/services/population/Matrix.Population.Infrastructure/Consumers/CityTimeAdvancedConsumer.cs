using MassTransit;
using MediatR;
using Matrix.CityCore.Contracts.Events;
using Matrix.Population.Application.UseCases.Population.AdvanceCityPopulation;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTimeAdvancedV1>
    {
        public async Task Consume(ConsumeContext<CityTimeAdvancedV1> context)
        {
            CityTimeAdvancedV1 message = context.Message;

            AdvanceCityPopulationResult result = await mediator.Send(
                request: new AdvanceCityPopulationCommand(
                    CityId: message.CityId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc,
                    TickId: message.TickId),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case AdvanceCityPopulationStatus.Applied:
                    logger.LogInformation(
                        message: "Applied city population progression for cityId={CityId}, tickId={TickId}, affectedPeople={AffectedPeople}.",
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
                    logger.LogWarning(
                        message: "Skipped out-of-order city population progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityPopulationStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city population progression for deleted cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
