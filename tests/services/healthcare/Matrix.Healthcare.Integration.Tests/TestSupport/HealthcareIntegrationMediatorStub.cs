using Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation;
using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;
using Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;
using MediatR;

namespace Matrix.Healthcare.Integration.Tests.TestSupport
{
    internal sealed class HealthcareIntegrationMediatorStub : IMediator
    {
        internal List<SynchronizePatientProfilesCommand> Commands { get; } = [];
        internal List<InitializePatientMedicalRecordsCommand> MedicalCommands { get; } = [];
        internal List<AdvancePatientHealthCommand> HealthProgressionCommands { get; } = [];
        internal List<DeleteHealthcareSimulationCommand> DeletionCommands { get; } = [];
        internal List<SynchronizeCareFacilitiesCommand> FacilityCommands { get; } = [];
        internal List<SynchronizeCareServiceQualityCommand> CareQualityCommands { get; } = [];
        internal List<SynchronizeCareMedicineSupplyCommand> MedicineSupplyCommands { get; } = [];

        internal SynchronizePatientProfilesResult Result { get; set; } = new(
            Status: SynchronizePatientProfilesStatus.Applied,
            AddedProfiles: 0,
            UpdatedProfiles: 0,
            IgnoredProfiles: 0);

        internal DeleteHealthcareSimulationResult DeletionResult { get; set; } =
            new(DeleteHealthcareSimulationStatus.Applied);

        internal SynchronizeCareFacilitiesResult FacilityResult { get; set; } = new(
            Status: SynchronizeCareFacilitiesStatus.Applied,
            AddedFacilities: 0,
            UpdatedFacilities: 0,
            IgnoredFacilities: 0);

        internal InitializePatientMedicalRecordsResult MedicalResult { get; set; } = new(
            Status: InitializePatientMedicalRecordsStatus.Applied,
            AddedRecords: 0,
            IgnoredRecords: 0);

        internal AdvancePatientHealthResult HealthProgressionResult { get; set; } = new(
            AdvancePatientHealthStatus.Applied,
            ProcessedPatients: 0,
            IgnoredPatients: 0,
            StalePatients: 0,
            Outcomes: Array.Empty<PatientHealthProgressionResultItem>());

        internal SynchronizeCareServiceQualityResult CareQualityResult { get; set; } = new(
            SynchronizeCareServiceQualityStatus.Applied,
            StateCreated: false,
            StateUpdated: false);

        internal SynchronizeCareMedicineSupplyResult MedicineSupplyResult { get; set; } = new(
            SynchronizeCareMedicineSupplyStatus.Applied,
            StateCreated: false,
            StateUpdated: false);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                SynchronizePatientProfilesCommand command => RecordProfile(command),
                InitializePatientMedicalRecordsCommand command => RecordMedical(command),
                AdvancePatientHealthCommand command => RecordHealthProgression(command),
                DeleteHealthcareSimulationCommand command => RecordDeletion(command),
                SynchronizeCareFacilitiesCommand command => RecordFacilitySynchronization(command),
                SynchronizeCareServiceQualityCommand command => RecordCareQuality(command),
                SynchronizeCareMedicineSupplyCommand command => RecordMedicineSupply(command),
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

        private SynchronizeCareFacilitiesResult RecordFacilitySynchronization(
            SynchronizeCareFacilitiesCommand command)
        {
            FacilityCommands.Add(command);
            return FacilityResult;
        }

        private AdvancePatientHealthResult RecordHealthProgression(AdvancePatientHealthCommand command)
        {
            HealthProgressionCommands.Add(command);
            return HealthProgressionResult;
        }

        private SynchronizeCareServiceQualityResult RecordCareQuality(
            SynchronizeCareServiceQualityCommand command)
        {
            CareQualityCommands.Add(command);
            return CareQualityResult;
        }

        private SynchronizeCareMedicineSupplyResult RecordMedicineSupply(
            SynchronizeCareMedicineSupplyCommand command)
        {
            MedicineSupplyCommands.Add(command);
            return MedicineSupplyResult;
        }
    }
}
