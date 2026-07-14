using Matrix.Education.Application.Lifecycle.DeleteEducationSimulation;
using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Application.Progression;
using Matrix.Education.Application.Progression.AdvanceEducationProgression;
using MediatR;

namespace Matrix.Education.Integration.Tests.TestSupport
{
    internal sealed class EducationIntegrationMediatorStub : IMediator
    {
        internal List<SynchronizeStudentProfilesCommand> ProfileCommands { get; } = [];
        internal List<SynchronizeEducationInstitutionsCommand> InstitutionCommands { get; } = [];
        internal List<DeleteEducationSimulationCommand> DeletionCommands { get; } = [];
        internal List<AdvanceEducationProgressionCommand> ProgressionCommands { get; } = [];

        internal SynchronizeStudentProfilesResult ProfileResult { get; set; } = new(
            Status: SynchronizeStudentProfilesStatus.Applied,
            AddedProfiles: 0,
            UpdatedProfiles: 0,
            IgnoredProfiles: 0);

        internal DeleteEducationSimulationResult DeletionResult { get; set; } =
            new(DeleteEducationSimulationStatus.Applied);

        internal SynchronizeEducationInstitutionsResult InstitutionResult { get; set; } = new(
            Status: SynchronizeEducationInstitutionsStatus.Applied,
            AddedInstitutions: 0,
            UpdatedInstitutions: 0,
            IgnoredInstitutions: 0);

        internal AdvanceEducationProgressionResult ProgressionResult { get; set; } = new(
            Status: AdvanceEducationProgressionStatus.Applied,
            BatchResult: EducationProgressionBatchResult.Empty);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                SynchronizeStudentProfilesCommand command => RecordProfile(command),
                SynchronizeEducationInstitutionsCommand command => RecordInstitutions(command),
                DeleteEducationSimulationCommand command => RecordDeletion(command),
                AdvanceEducationProgressionCommand command => RecordProgression(command),
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

        private SynchronizeEducationInstitutionsResult RecordInstitutions(
            SynchronizeEducationInstitutionsCommand command)
        {
            InstitutionCommands.Add(command);
            return InstitutionResult;
        }

        private AdvanceEducationProgressionResult RecordProgression(
            AdvanceEducationProgressionCommand command)
        {
            ProgressionCommands.Add(command);
            return ProgressionResult;
        }
    }
}
