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
            return type switch
            {
                HealthcareOutboxEventTypes.PatientHealthOutcomeBatchV1 =>
                    PublishAsync<HealthcarePatientHealthOutcomeBatchV1>(payloadJson, cancellationToken),
                HealthcareOutboxEventTypes.CareDeliveryActivityV1 =>
                    PublishAsync<HealthcareCareDeliveryActivityV1>(payloadJson, cancellationToken),
                HealthcareOutboxEventTypes.PopulationHealthSnapshotV1 =>
                    PublishAsync<HealthcarePopulationHealthSnapshotV1>(payloadJson, cancellationToken),
                _ => throw new NotSupportedException(
                    $"Healthcare outbox message type '{type}' is not supported.")
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
                $"Failed to deserialize healthcare outbox payload as '{typeof(TEvent).Name}'.");

            return publishEndpoint.Publish(
                message: integrationEvent,
                cancellationToken: cancellationToken);
        }
    }
}
