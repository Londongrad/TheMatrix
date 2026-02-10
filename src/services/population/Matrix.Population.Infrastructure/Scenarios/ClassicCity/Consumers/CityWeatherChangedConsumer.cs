using MassTransit;
using Matrix.CityCore.Contracts.Events;
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
        public async Task Consume(ConsumeContext<CityWeatherChangedV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException("CityWeatherChanged message must have a MessageId.");

            CityWeatherChangedV1 message = context.Message;

            ApplyCityWeatherImpactResult result = await mediator.Send(
                request: new ApplyCityWeatherImpactCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
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
                cancellationToken: context.CancellationToken);

            SyncCityWeatherExposureStateResult syncResult = await mediator.Send(
                request: new SyncCityWeatherExposureStateCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
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
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityWeatherImpactStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied city weather impact for cityId={CityId}, messageId={MessageId}, affectedPeople={AffectedPeople}.",
                        message.CityId,
                        context.MessageId,
                        result.AffectedPeopleCount);
                    break;

                case ApplyCityWeatherImpactStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city weather impact for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityWeatherImpactStatus.OutOfOrder:
                    logger.LogWarning(
                        message: "Skipped out-of-order city weather impact for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityWeatherImpactStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city weather impact for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityWeatherImpactStatus.CityArchived:
                    logger.LogDebug(
                        message: "Skipped city weather impact for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }

            switch (syncResult.Status)
            {
                case SyncCityWeatherExposureStateStatus.OutOfOrder:
                    logger.LogWarning(
                        message:
                        "Skipped out-of-order city weather exposure sync for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure sync for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case SyncCityWeatherExposureStateStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped city weather exposure sync for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
