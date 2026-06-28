using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.Healthcare.Contracts.Events;

namespace Matrix.Healthcare.Infrastructure.Outbox.RabbitMq
{
    public sealed class HealthcareMassTransitOutboxMessagePublisher(IPublishEndpoint publishEndpoint)
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
            if (!string.Equals(
                    type,
                    HealthcareOutboxEventTypes.PatientHealthOutcomeBatchV1,
                    StringComparison.Ordinal))
                throw new NotSupportedException($"Healthcare outbox message type '{type}' is not supported.");

            HealthcarePatientHealthOutcomeBatchV1 integrationEvent =
                JsonSerializer.Deserialize<HealthcarePatientHealthOutcomeBatchV1>(
                    payloadJson,
                    JsonOptions) ??
                throw new InvalidOperationException(
                    $"Failed to deserialize healthcare outbox payload for type '{type}'.");

            return publishEndpoint.Publish(
                message: integrationEvent,
                cancellationToken: cancellationToken);
        }
    }
}
