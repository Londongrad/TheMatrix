using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityServiceQualitySnapshotConsumer(
        IMediator mediator,
        ILogger<ClassicCityServiceQualitySnapshotConsumer> logger)
        : IConsumer<ClassicCityServiceQualitySnapshotV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityServiceQualitySnapshotV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityServiceQualitySnapshot message must have a MessageId.");

            ClassicCityServiceQualitySnapshotV1 message = context.Message;

            ApplyCityServiceQualitySnapshotResult result = await mediator.Send(
                request: new ApplyCityServiceQualitySnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: ClassicCityServiceQualitySnapshotConsumerDefinition.EndpointNameValue,
                    HealthcareQualityIndex: message.HealthcareQualityIndex,
                    EducationQualityIndex: message.EducationQualityIndex,
                    HousingSupportIndex: message.HousingSupportIndex,
                    OccurredAtUtc: message.OccurredAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityServiceQualitySnapshotStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city service-quality snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city service-quality snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
