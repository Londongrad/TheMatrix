using Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation;
using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using MediatR;

namespace Matrix.Healthcare.Integration.Tests.TestSupport
{
    internal sealed class HealthcareIntegrationMediatorStub : IMediator
    {
        internal List<SynchronizePatientProfilesCommand> Commands { get; } = [];
        internal List<InitializePatientMedicalRecordsCommand> MedicalCommands { get; } = [];
        internal List<DeleteHealthcareSimulationCommand> DeletionCommands { get; } = [];

        internal SynchronizePatientProfilesResult Result { get; set; } = new(
            Status: SynchronizePatientProfilesStatus.Applied,
            AddedProfiles: 0,
            UpdatedProfiles: 0,
            IgnoredProfiles: 0);

        internal DeleteHealthcareSimulationResult DeletionResult { get; set; } =
            new(DeleteHealthcareSimulationStatus.Applied);

        internal InitializePatientMedicalRecordsResult MedicalResult { get; set; } = new(
            Status: InitializePatientMedicalRecordsStatus.Applied,
            AddedRecords: 0,
            IgnoredRecords: 0);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                SynchronizePatientProfilesCommand command => RecordProfile(command),
                InitializePatientMedicalRecordsCommand command => RecordMedical(command),
                DeleteHealthcareSimulationCommand command => RecordDeletion(command),
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

        private SynchronizePatientProfilesResult RecordProfile(SynchronizePatientProfilesCommand command)
        {
            Commands.Add(command);
            return Result;
        }

        private DeleteHealthcareSimulationResult RecordDeletion(DeleteHealthcareSimulationCommand command)
        {
            DeletionCommands.Add(command);
            return DeletionResult;
        }

        private InitializePatientMedicalRecordsResult RecordMedical(
            InitializePatientMedicalRecordsCommand command)
        {
            MedicalCommands.Add(command);
            return MedicalResult;
        }
    }
}
