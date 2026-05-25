using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherCreatedConsumer(
        IMediator mediator,
        ILogger<CityWeatherCreatedConsumer> logger) : IConsumer<CityWeatherCreatedV1>
    {
        public Task Consume(ConsumeContext<CityWeatherCreatedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityWeatherCreatedV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException("CityWeatherCreated message must have a MessageId.");

            SyncCityWeatherExposureStateResult result = await mediator.Send(
                request: new SyncCityWeatherExposureStateCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
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
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case SyncCityWeatherExposureStateStatus.Applied:
                    logger.LogInformation(
                        message: "Initialized city weather exposure state for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate city weather exposure initialization for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order city weather exposure initialization for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure initialization for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure initialization for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
