using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<CityDeletedV1>
    {
        public Task Consume(ConsumeContext<CityDeletedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityDeletedV1 message,
            CancellationToken cancellationToken)
        {
            DeleteCitySystemsDataResult result = await mediator.Send(
                request: new DeleteCitySystemsDataCommand(
                    CityId: message.CityId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteCitySystemsDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted simulation systems data for cityId={CityId}.",
                        message.CityId);
                    break;
                case DeleteCitySystemsDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate simulation systems deletion for cityId={CityId}.",
                        message.CityId);
                    break;
                case DeleteCitySystemsDataStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale simulation systems deletion for cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
