using MassTransit;
using Matrix.Economy.Application.UseCases.Lifecycle.DeleteCityEconomyData;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<CityDeletedV1>
    {
        public async Task Consume(ConsumeContext<CityDeletedV1> context)
        {
            DeleteCityEconomyDataResult result = await mediator.Send(
                request: new DeleteCityEconomyDataCommand(
                    CityId: context.Message.CityId,
                    DeletedAtUtc: context.Message.DeletedAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case DeleteCityEconomyDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted economy data for cityId={CityId}.",
                        context.Message.CityId);
                    break;
                case DeleteCityEconomyDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate economy deletion for cityId={CityId}.",
                        context.Message.CityId);
                    break;
                case DeleteCityEconomyDataStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale economy deletion for cityId={CityId}.",
                        context.Message.CityId);
                    break;
            }
        }
    }
}
