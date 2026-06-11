using System.Text.Json;
using MassTransit;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;

namespace Matrix.SimulationSystems.Infrastructure.Outbox.RabbitMq
{
    public sealed class MassTransitOutboxMessagePublisher(
        IPublishEndpoint publishEndpoint,
        OutboxEventTypeRegistry eventTypeRegistry)
        : IOutboxMessagePublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            Type eventType = eventTypeRegistry.Resolve(type);

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
