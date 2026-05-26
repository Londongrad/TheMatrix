using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
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
            DeleteCityResourcesResult result = await mediator.Send(
                request: new DeleteCityResourcesCommand(
                    CityId: message.CityId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteCityResourcesStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted resource data for cityId={CityId}.",
                        message.CityId);
                    break;
                case DeleteCityResourcesStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate resource deletion for cityId={CityId}.",
                        message.CityId);
                    break;
                case DeleteCityResourcesStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale resource deletion for cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
