using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsMappedSynchronizationCommand()
        {
            var mediator = new RecordingMediator
            {
                Result = new SynchronizeStudentProfilesResult(
                    AddedProfiles: 1,
                    UpdatedProfiles: 0,
                    IgnoredProfiles: 0)
            };
            var consumer = new PopulationResidentFactsConsumer(
                mediator: mediator,
                logger: NullLogger<PopulationResidentFactsConsumer>.Instance);
            var message = new PopulationResidentFactsBatchV1(
                SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                SourceRevision: 14,
                SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "resident-facts:14",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentFactsV1(
                        ResidentId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                        BirthDate: new DateOnly(2027, 4, 3),
                        Sex: "Female",
                        IsAlive: true,
                        IsActive: true)
                ]);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SynchronizeStudentProfilesCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.SimulationHostId,
                actual: command.SimulationHostId);
            SynchronizeStudentProfileItem profile = Assert.Single(command.Profiles);
            Assert.Equal(
                expected: message.Residents[0].ResidentId,
                actual: profile.ResidentId);
            Assert.Equal(
                expected: message.SourceRevision,
                actual: profile.SourceRevision);
        }

        private sealed class RecordingMediator : IMediator
        {
            public List<SynchronizeStudentProfilesCommand> Commands { get; } = [];
            public required SynchronizeStudentProfilesResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Commands.Add(Assert.IsType<SynchronizeStudentProfilesCommand>(request));
                return Task.FromResult((TResponse)(object)Result);
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                throw new NotSupportedException();
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                throw new NotSupportedException();
            }
        }
    }
}
