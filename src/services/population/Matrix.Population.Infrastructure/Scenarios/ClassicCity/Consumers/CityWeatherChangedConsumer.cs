using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherChangedConsumer(
        IMediator mediator,
        ILogger<CityWeatherChangedConsumer> logger) : IConsumer<CityWeatherChangedV1>
    {
        public Task Consume(ConsumeContext<CityWeatherChangedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityWeatherChangedV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException("CityWeatherChanged message must have a MessageId.");

            ApplyCityWeatherImpactResult result = await mediator.Send(
                request: new ApplyCityWeatherImpactCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: CityWeatherChangedConsumerDefinition.EndpointNameValue,
                    AtSimTimeUtc: message.AtSimTimeUtc,
                    OccurredOnUtc: message.OccurredOnUtc,
                    PreviousState: new WeatherImpactSnapshotInput(
                        Type: message.PreviousState.Type,
                        Severity: message.PreviousState.Severity,
                        PrecipitationKind: message.PreviousState.PrecipitationKind,
                        TemperatureC: message.PreviousState.TemperatureC,
                        HumidityPercent: message.PreviousState.HumidityPercent,
                        WindSpeedKph: message.PreviousState.WindSpeedKph,
                        CloudCoveragePercent: message.PreviousState.CloudCoveragePercent,
                        PressureHpa: message.PreviousState.PressureHpa),
                    CurrentState: new WeatherImpactSnapshotInput(
                        Type: message.CurrentState.Type,
                        Severity: message.CurrentState.Severity,
                        PrecipitationKind: message.CurrentState.PrecipitationKind,
                        TemperatureC: message.CurrentState.TemperatureC,
                        HumidityPercent: message.CurrentState.HumidityPercent,
                        WindSpeedKph: message.CurrentState.WindSpeedKph,
                        CloudCoveragePercent: message.CurrentState.CloudCoveragePercent,
                        PressureHpa: message.CurrentState.PressureHpa)),
                cancellationToken: cancellationToken);

            SyncCityWeatherExposureStateResult syncResult = await mediator.Send(
                request: new SyncCityWeatherExposureStateCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: $"{CityWeatherChangedConsumerDefinition.EndpointNameValue}-sync",
                    AtSimTimeUtc: message.AtSimTimeUtc,
                    OccurredOnUtc: message.OccurredOnUtc,
                    PreviousState: new WeatherImpactSnapshotInput(
                        Type: message.PreviousState.Type,
                        Severity: message.PreviousState.Severity,
                        PrecipitationKind: message.PreviousState.PrecipitationKind,
                        TemperatureC: message.PreviousState.TemperatureC,
                        HumidityPercent: message.PreviousState.HumidityPercent,
                        WindSpeedKph: message.PreviousState.WindSpeedKph,
                        CloudCoveragePercent: message.PreviousState.CloudCoveragePercent,
                        PressureHpa: message.PreviousState.PressureHpa),
                    CurrentState: new WeatherImpactSnapshotInput(
                        Type: message.CurrentState.Type,
                        Severity: message.CurrentState.Severity,
                        PrecipitationKind: message.CurrentState.PrecipitationKind,
                        TemperatureC: message.CurrentState.TemperatureC,
                        HumidityPercent: message.CurrentState.HumidityPercent,
                        WindSpeedKph: message.CurrentState.WindSpeedKph,
                        CloudCoveragePercent: message.CurrentState.CloudCoveragePercent,
                        PressureHpa: message.CurrentState.PressureHpa)),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityWeatherImpactStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied city weather impact for cityId={CityId}, messageId={MessageId}, affectedPeople={AffectedPeople}.",
                        message.CityId,
                        messageId,
                        result.AffectedPeopleCount);
                    break;

                case ApplyCityWeatherImpactStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city weather impact for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityWeatherImpactStatus.OutOfOrder:
                    logger.LogDebug(
                        message: "Skipped out-of-order city weather impact for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityWeatherImpactStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city weather impact for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityWeatherImpactStatus.CityArchived:
                    logger.LogDebug(
                        message: "Skipped city weather impact for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }

            switch (syncResult.Status)
            {
                case SyncCityWeatherExposureStateStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order city weather exposure sync for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure sync for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure sync for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
