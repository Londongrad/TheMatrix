using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.Education.Contracts.Events;

namespace Matrix.Education.Infrastructure.Outbox.RabbitMq
{
    public sealed class EducationMassTransitOutboxMessagePublisher(IPublishEndpoint publishEndpoint)
        : IOutboxMessagePublisher
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            return type switch
            {
                EducationOutboxEventTypes.StudentParticipationBatchV1 =>
                    PublishAsync<EducationStudentParticipationBatchV1>(
                        payloadJson,
                        cancellationToken),
                _ => throw new NotSupportedException(
                    $"Education outbox message type '{type}' is not supported.")
            };
        }

        private Task PublishAsync<TEvent>(
            string payloadJson,
            CancellationToken cancellationToken)
            where TEvent : class
        {
            TEvent integrationEvent = JsonSerializer.Deserialize<TEvent>(
                payloadJson,
                JsonOptions) ?? throw new InvalidOperationException(
                $"Failed to deserialize education outbox payload as '{typeof(TEvent).Name}'.");

            return publishEndpoint.Publish(
                message: integrationEvent,
                cancellationToken: cancellationToken);
        }
    }
}
