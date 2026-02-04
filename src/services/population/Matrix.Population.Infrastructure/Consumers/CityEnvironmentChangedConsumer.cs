using MassTransit;
using MediatR;
using Matrix.CityCore.Contracts.Events;
using Matrix.Population.Application.UseCases.Population.SyncCityEnvironment;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityEnvironmentChangedConsumer(
        IMediator mediator,
        ILogger<CityEnvironmentChangedConsumer> logger) : IConsumer<CityEnvironmentChangedV1>
    {
        public async Task Consume(ConsumeContext<CityEnvironmentChangedV1> context)
        {
            CityEnvironmentChangedV1 message = context.Message;

            SyncCityEnvironmentResult result = await mediator.Send(
                request: new ApplyCityEnvironmentSyncCommand(
                    CityId: message.CityId,
                    ClimateZone: message.CurrentEnvironment.ClimateZone,
                    Hemisphere: message.CurrentEnvironment.Hemisphere,
                    UtcOffsetMinutes: message.CurrentEnvironment.UtcOffsetMinutes,
                    SyncedAtUtc: message.OccurredOnUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SyncCityEnvironmentStatus.Applied:
                    logger.LogInformation(
                        message: "Applied city environment sync for cityId={CityId}.",
                        message.CityId);
                    break;

                case SyncCityEnvironmentStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city environment sync for cityId={CityId}.",
                        message.CityId);
                    break;

                case SyncCityEnvironmentStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale city environment sync for cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
