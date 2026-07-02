using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed class AdvancePatientHealthCommandHandler(
        IPatientProfileRepository patientProfileRepository,
        IPatientMedicalRecordRepository medicalRecordRepository,
        IPatientCareNeedRepository patientCareNeedRepository,
        IHealthcareSimulationDeletionRepository deletionRepository,
        IPatientHealthOutcomeOutboxWriter outcomeOutboxWriter,
        PatientIllnessProgressionPolicy progressionPolicy,
        PatientCareNeedAssessmentPolicy careNeedAssessmentPolicy,
        IHealthcareUnitOfWork unitOfWork)
        : IRequestHandler<AdvancePatientHealthCommand, AdvancePatientHealthResult>
    {
        public const int MaxBatchSize = AdvancePatientHealthBatchPreparer.MaxBatchSize;

        public Task<AdvancePatientHealthResult> Handle(
            AdvancePatientHealthCommand request,
            CancellationToken cancellationToken)
        {
            PreparedPatientHealthBatch batch = AdvancePatientHealthBatchPreparer.Prepare(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => AdvanceInsideTransactionAsync(batch, token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<AdvancePatientHealthResult> AdvanceInsideTransactionAsync(
            PreparedPatientHealthBatch batch,
            CancellationToken cancellationToken)
        {
            DateTimeOffset? deletedAtUtc = await deletionRepository.GetDeletedAtUtcAsync(
                batch.SimulationHostId,
                cancellationToken);
            if (deletedAtUtc is not null)
                return new AdvancePatientHealthResult(
                    AdvancePatientHealthStatus.SimulationDeleted,
                    ProcessedPatients: 0,
                    IgnoredPatients: batch.Patients.Count,
                    StalePatients: 0,
                    Outcomes: Array.Empty<PatientHealthProgressionResultItem>());

            IReadOnlyList<PatientProfile> profiles = await patientProfileRepository.GetByIdsAsync(
                batch.PatientIds,
                cancellationToken);
            IReadOnlyList<PatientMedicalRecord> records = await medicalRecordRepository.GetByIdsAsync(
                batch.PatientIds,
                cancellationToken);
            IReadOnlyList<PatientCareNeed> careNeeds = await patientCareNeedRepository.GetByPatientIdsAsync(
                batch.PatientIds,
                cancellationToken);
            Dictionary<PatientId, PatientProfile> profilesById = profiles.ToDictionary(
                profile => profile.PatientId);
            Dictionary<PatientId, PatientMedicalRecord> recordsById = records.ToDictionary(
                record => record.PatientId);
            Dictionary<PatientId, PatientCareNeed> careNeedsByPatientId = careNeeds.ToDictionary(
                careNeed => careNeed.PatientId);
            var addedCareNeeds = new List<PatientCareNeed>();
            var outcomes = new List<PatientHealthProgressionResultItem>();
            int processedPatients = 0;
            int ignoredPatients = 0;
            int stalePatients = 0;

            foreach (PreparedPatientHealthRisk patient in batch.Patients)
            {
                if (!profilesById.TryGetValue(patient.PatientId, out PatientProfile? profile)
                    || !recordsById.TryGetValue(patient.PatientId, out PatientMedicalRecord? record)
                    || !profile.IsEligibleForCare)
                {
                    ignoredPatients++;
                    continue;
                }

                EnsureSameSimulationHost(batch.SimulationHostId, profile, record);

                if (patient.LifecycleRevision != profile.LastLifecycleRevision
                    || patient.LifecycleRevision != record.LastLifecycleRevision)
                {
                    stalePatients++;
                    continue;
                }

                if (!record.TryAcceptProgressionRevision(batch.SourceRevision))
                {
                    stalePatients++;
                    continue;
                }

                processedPatients++;
                PatientIllnessProgressionOutcome outcome = progressionPolicy.Apply(
                    record,
                    patient.RiskFactors,
                    batch.PreviousDate,
                    batch.CurrentDate);

                if (outcome.HasAnyEffect)
                    outcomes.Add(MapOutcome(record, outcome, patient.LifecycleRevision));

                PatientCareNeedAssessment assessment = careNeedAssessmentPolicy.Assess(record);
                if (careNeedsByPatientId.TryGetValue(patient.PatientId, out PatientCareNeed? careNeed))
                {
                    careNeed.TrySynchronizeAssessment(
                        simulationHostId: batch.SimulationHostId,
                        urgency: assessment.Urgency,
                        assessmentDate: batch.CurrentDate,
                        assessmentRevision: batch.SourceRevision,
                        lifecycleRevision: patient.LifecycleRevision,
                        assessedAtUtc: batch.ObservedAtUtc);
                }
                else if (assessment.Urgency.HasValue)
                {
                    addedCareNeeds.Add(PatientCareNeed.Register(
                        patientId: patient.PatientId,
                        simulationHostId: batch.SimulationHostId,
                        urgency: assessment.Urgency.Value,
                        requestedOn: batch.CurrentDate,
                        assessmentRevision: batch.SourceRevision,
                        lifecycleRevision: patient.LifecycleRevision,
                        assessedAtUtc: batch.ObservedAtUtc));
                }
            }

            if (processedPatients > 0)
            {
                if (addedCareNeeds.Count > 0)
                    await patientCareNeedRepository.AddRangeAsync(
                        careNeeds: addedCareNeeds,
                        cancellationToken: cancellationToken);

                if (outcomes.Count > 0)
                    await outcomeOutboxWriter.AddAsync(
                        new PatientHealthOutcomeBatch(
                            SimulationHostId: batch.SimulationHostId.Value,
                            SourceRevision: batch.SourceRevision,
                            CurrentDate: batch.CurrentDate,
                            OccurredAtUtc: batch.ObservedAtUtc,
                            CorrelationId: batch.CorrelationId,
                            BatchNumber: batch.BatchNumber,
                            TotalBatches: batch.TotalBatches,
                            Patients: outcomes),
                        cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new AdvancePatientHealthResult(
                AdvancePatientHealthStatus.Applied,
                ProcessedPatients: processedPatients,
                IgnoredPatients: ignoredPatients,
                StalePatients: stalePatients,
                Outcomes: outcomes);
        }

        private static void EnsureSameSimulationHost(
            SimulationHostId expected,
            PatientProfile profile,
            PatientMedicalRecord record)
        {
            if (profile.SimulationHostId != expected || record.SimulationHostId != expected)
                throw new InvalidOperationException(
                    $"Patient '{record.PatientId}' does not belong to the requested simulation host.");
        }

        private static PatientHealthProgressionResultItem MapOutcome(
            PatientMedicalRecord record,
            PatientIllnessProgressionOutcome outcome,
            long lifecycleRevision)
        {
            return new PatientHealthProgressionResultItem(
                PatientId: record.PatientId.Value,
                HealthScore: record.Health.Value,
                CurrentIllnessKind: record.Illness.CurrentKind,
                CurrentIllnessSeverity: record.Illness.CurrentSeverity,
                DiagnosedOn: record.Illness.DiagnosedOn,
                LastRecoveredOn: record.Illness.LastRecoveredOn,
                HealthDelta: outcome.HealthDelta,
                HappinessDelta: outcome.HappinessDelta,
                EnergyDelta: outcome.EnergyDelta,
                StressDelta: outcome.StressDelta,
                BecameCritical: outcome.BecameCritical,
                LifecycleRevision: lifecycleRevision);
        }

    }
}
