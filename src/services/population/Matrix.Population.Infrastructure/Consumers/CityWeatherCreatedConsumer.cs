using MassTransit;
using Matrix.CityCore.Contracts.Events;
using Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.UseCases.Population.SyncCityWeatherExposureState;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityWeatherCreatedConsumer(
        IMediator mediator,
        ILogger<CityWeatherCreatedConsumer> logger) : IConsumer<CityWeatherCreatedV1>
    {
        public async Task Consume(ConsumeContext<CityWeatherCreatedV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException("CityWeatherCreated message must have a MessageId.");

            CityWeatherCreatedV1 message = context.Message;

            SyncCityWeatherExposureStateResult result = await mediator.Send(
                request: new SyncCityWeatherExposureStateCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: CityWeatherCreatedConsumerDefinition.EndpointNameValue,
                    AtSimTimeUtc: message.AtSimTimeUtc,
                    OccurredOnUtc: message.OccurredOnUtc,
                    PreviousState: null,
                    CurrentState: new WeatherImpactSnapshotInput(
                        Type: message.InitialState.Type,
                        Severity: message.InitialState.Severity,
                        PrecipitationKind: message.InitialState.PrecipitationKind,
                        TemperatureC: message.InitialState.TemperatureC,
                        HumidityPercent: message.InitialState.HumidityPercent,
                        WindSpeedKph: message.InitialState.WindSpeedKph,
                        CloudCoveragePercent: message.InitialState.CloudCoveragePercent,
                        PressureHpa: message.InitialState.PressureHpa)),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SyncCityWeatherExposureStateStatus.Applied:
                    logger.LogInformation(
                        message: "Initialized city weather exposure state for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate city weather exposure initialization for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.OutOfOrder:
                    logger.LogWarning(
                        message:
                        "Skipped out-of-order city weather exposure initialization for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure initialization for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure initialization for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
