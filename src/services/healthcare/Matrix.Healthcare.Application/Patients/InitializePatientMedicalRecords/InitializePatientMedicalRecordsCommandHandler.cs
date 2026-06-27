using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords
{
    public sealed class InitializePatientMedicalRecordsCommandHandler(
        IPatientMedicalRecordRepository medicalRecordRepository,
        IHealthcareSimulationDeletionRepository deletionRepository,
        IHealthcareUnitOfWork unitOfWork)
        : IRequestHandler<InitializePatientMedicalRecordsCommand, InitializePatientMedicalRecordsResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<InitializePatientMedicalRecordsResult> Handle(
            InitializePatientMedicalRecordsCommand request,
            CancellationToken cancellationToken)
        {
            PreparedBatch batch = Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => InitializeInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<InitializePatientMedicalRecordsResult> InitializeInsideTransactionAsync(
            PreparedBatch batch,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                simulationHostId: batch.SimulationHostId,
                cancellationToken: cancellationToken);

            if (deletedAtUtc is not null)
                return new InitializePatientMedicalRecordsResult(
                    Status: InitializePatientMedicalRecordsStatus.SimulationDeleted,
                    AddedRecords: 0,
                    IgnoredRecords: batch.Records.Count);

            IReadOnlyList<PatientMedicalRecord> existingRecords =
                await medicalRecordRepository.GetByIdsAsync(
                    patientIds: batch.PatientIds,
                    cancellationToken: cancellationToken);
            Dictionary<PatientId, PatientMedicalRecord> existingById = existingRecords.ToDictionary(
                record => record.PatientId);
            var addedRecords = new List<PatientMedicalRecord>();

            foreach (PreparedMedicalRecord prepared in batch.Records)
            {
                if (existingById.TryGetValue(prepared.PatientId, out PatientMedicalRecord? existing))
                {
                    if (existing.SimulationHostId != batch.SimulationHostId)
                        throw new InvalidOperationException(
                            $"Patient '{prepared.PatientId}' already belongs to another simulation host.");

                    continue;
                }

                addedRecords.Add(PatientMedicalRecord.Register(
                    patientId: prepared.PatientId,
                    simulationHostId: batch.SimulationHostId,
                    health: prepared.Health,
                    illness: prepared.Illness));
            }

            if (addedRecords.Count > 0)
            {
                await medicalRecordRepository.AddRangeAsync(
                    records: addedRecords,
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new InitializePatientMedicalRecordsResult(
                Status: InitializePatientMedicalRecordsStatus.Applied,
                AddedRecords: addedRecords.Count,
                IgnoredRecords: batch.Records.Count - addedRecords.Count);
        }

        private static PreparedBatch Prepare(InitializePatientMedicalRecordsCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Records);

            if (request.Records.Count == 0)
                throw new ArgumentException(
                    message: "A medical record initialization batch cannot be empty.",
                    paramName: nameof(request.Records));

            if (request.Records.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.Records),
                    message: $"A medical record initialization batch cannot exceed {MaxBatchSize} records.");

            if (request.ObservedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Medical state observation timestamps must be expressed in UTC.",
                    paramName: nameof(request.ObservedAtUtc));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var patientIds = new HashSet<PatientId>();
            var preparedRecords = new PreparedMedicalRecord[request.Records.Count];

            for (int index = 0; index < request.Records.Count; index++)
            {
                InitializePatientMedicalRecordItem item = request.Records[index] ??
                                                          throw new ArgumentException(
                                                              "A batch cannot contain null medical records.",
                                                              nameof(request.Records));
                var patientId = new PatientId(item.PatientId);

                if (!patientIds.Add(patientId))
                    throw new ArgumentException(
                        message: $"Patient '{patientId}' occurs more than once in an initialization batch.",
                        paramName: nameof(request.Records));

                preparedRecords[index] = new PreparedMedicalRecord(
                    PatientId: patientId,
                    Health: new HealthScore(item.HealthScore),
                    Illness: CreateIllnessState(item));
            }

            return new PreparedBatch(
                SimulationHostId: simulationHostId,
                PatientIds: patientIds.ToArray(),
                Records: preparedRecords);
        }

        private static PatientIllnessState CreateIllnessState(InitializePatientMedicalRecordItem item)
        {
            if (item.CurrentIllnessKind is null
                && item.CurrentIllnessSeverity is null
                && item.DiagnosedOn is null)
                return PatientIllnessState.Healthy(item.LastRecoveredOn);

            if (item.CurrentIllnessKind.HasValue
                && item.CurrentIllnessSeverity.HasValue
                && item.DiagnosedOn.HasValue)
                return PatientIllnessState.Active(
                    kind: item.CurrentIllnessKind.Value,
                    severity: item.CurrentIllnessSeverity.Value,
                    diagnosedOn: item.DiagnosedOn.Value,
                    lastRecoveredOn: item.LastRecoveredOn);

            throw new ArgumentException(
                message: "Illness kind, severity, and diagnosis date must be present together.",
                paramName: nameof(item));
        }

        private sealed record PreparedBatch(
            SimulationHostId SimulationHostId,
            IReadOnlyCollection<PatientId> PatientIds,
            IReadOnlyList<PreparedMedicalRecord> Records);

        private sealed record PreparedMedicalRecord(
            PatientId PatientId,
            HealthScore Health,
            PatientIllnessState Illness);
    }
}
