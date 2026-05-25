using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;

namespace Matrix.SimulationSystems.Infrastructure.Outbox.RabbitMq
{
    public sealed class MassTransitOutboxMessagePublisher(IPublishEndpoint publishEndpoint)
        : IOutboxMessagePublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            if (!OutboxEventTypeMap.Map.TryGetValue(
                    key: type,
                    value: out Type? eventType))
                throw new NotSupportedException($"Outbox message type '{type}' is not supported.");

            object? message = JsonSerializer.Deserialize(
                json: payloadJson,
                returnType: eventType,
                options: JsonOptions);

            if (message is null)
                throw new InvalidOperationException($"Failed to deserialize outbox payload for type '{type}'.");

            return publishEndpoint.Publish(
                message: message,
                messageType: eventType,
                cancellationToken: cancellationToken);
        }
    }
}
