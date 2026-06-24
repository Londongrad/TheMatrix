using Matrix.Education.Application.Lifecycle.DeleteEducationSimulation;
using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using MediatR;

namespace Matrix.Education.Integration.Tests.TestSupport
{
    internal sealed class EducationIntegrationMediatorStub : IMediator
    {
        internal List<SynchronizeStudentProfilesCommand> ProfileCommands { get; } = [];
        internal List<DeleteEducationSimulationCommand> DeletionCommands { get; } = [];

        internal SynchronizeStudentProfilesResult ProfileResult { get; set; } = new(
            Status: SynchronizeStudentProfilesStatus.Applied,
            AddedProfiles: 0,
            UpdatedProfiles: 0,
            IgnoredProfiles: 0);

        internal DeleteEducationSimulationResult DeletionResult { get; set; } =
            new(DeleteEducationSimulationStatus.Applied);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                SynchronizeStudentProfilesCommand command => RecordProfile(command),
                DeleteEducationSimulationCommand command => RecordDeletion(command),
                _ => throw new NotSupportedException(request.GetType().FullName)
            };

            return Task.FromResult((TResponse)response);
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

        private SynchronizeStudentProfilesResult RecordProfile(SynchronizeStudentProfilesCommand command)
        {
            ProfileCommands.Add(command);
            return ProfileResult;
        }

        private DeleteEducationSimulationResult RecordDeletion(DeleteEducationSimulationCommand command)
        {
            DeletionCommands.Add(command);
            return DeletionResult;
        }
    }
}
