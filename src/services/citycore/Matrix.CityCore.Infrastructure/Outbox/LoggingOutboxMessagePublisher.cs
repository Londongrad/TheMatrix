using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Microsoft.Extensions.Logging;

namespace Matrix.CityCore.Infrastructure.Outbox
{
    public sealed class LoggingOutboxMessagePublisher(ILogger<LoggingOutboxMessagePublisher> logger)
        : IOutboxMessagePublisher
    {
        public Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                message: "CityCore outbox publish messageId={MessageId}, type={Type}",
                messageId,
                type);

            return Task.CompletedTask;
        }
    }
}
