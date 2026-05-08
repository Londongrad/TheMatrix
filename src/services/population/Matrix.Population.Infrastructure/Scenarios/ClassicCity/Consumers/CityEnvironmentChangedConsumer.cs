using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityEnvironmentChangedConsumer(
        IMediator mediator,
        ILogger<CityEnvironmentChangedConsumer> logger) : IConsumer<CityEnvironmentChangedV1>
    {
        public Task Consume(ConsumeContext<CityEnvironmentChangedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityEnvironmentChangedV1 message,
            CancellationToken cancellationToken)
        {
            SyncCityEnvironmentResult result = await mediator.Send(
                request: new ApplyCityEnvironmentSyncCommand(
                    CityId: message.CityId,
                    ClimateZone: message.CurrentEnvironment.ClimateZone,
                    Hemisphere: message.CurrentEnvironment.Hemisphere,
                    UtcOffsetMinutes: message.CurrentEnvironment.UtcOffsetMinutes,
                    SyncedAtUtc: message.OccurredOnUtc),
                cancellationToken: cancellationToken);

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

                case SyncCityEnvironmentStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city environment sync for deleted cityId={CityId}.",
                        message.CityId);
                    break;

                case SyncCityEnvironmentStatus.CityArchived:
                    logger.LogDebug(
                        message: "Skipped city environment sync for archived cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
