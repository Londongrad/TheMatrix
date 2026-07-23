using System.Text.Json;
using Matrix.Education.Infrastructure.Outbox;
using Matrix.Education.Infrastructure.Outbox.RabbitMq;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Outbox
{
    public sealed class EducationMassTransitOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_UnsupportedMessageType_Throws()
        {
            var publisher = new EducationMassTransitOutboxMessagePublisher(null!);

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
                publisher.PublishAsync(
                    Guid.NewGuid(),
                    "education.unknown.v1",
                    "{}",
                    CancellationToken.None));

            Assert.Contains("not supported", exception.Message);
        }

        [Theory]
        [InlineData(EducationOutboxEventTypes.StudentParticipationBatchV1)]
        [InlineData(EducationOutboxEventTypes.AttendanceEvaluatedBatchV1)]
        public async Task PublishAsync_NullPayload_Throws(string eventType)
        {
            var publisher = new EducationMassTransitOutboxMessagePublisher(null!);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(
                    Guid.NewGuid(),
                    eventType,
                    "null",
                    CancellationToken.None));

            Assert.Contains("Failed to deserialize education outbox payload", exception.Message);
        }

        [Theory]
        [InlineData(EducationOutboxEventTypes.StudentParticipationBatchV1)]
        [InlineData(EducationOutboxEventTypes.AttendanceEvaluatedBatchV1)]
        public async Task PublishAsync_MalformedPayload_Throws(string eventType)
        {
            var publisher = new EducationMassTransitOutboxMessagePublisher(null!);

            await Assert.ThrowsAsync<JsonException>(() => publisher.PublishAsync(
                Guid.NewGuid(),
                eventType,
                "{",
                CancellationToken.None));
        }
    }
}
