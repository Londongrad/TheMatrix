using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care;
using Matrix.Healthcare.Application.Care.DeliverPatientCare;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using MediatR;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed class AdvancePatientHealthCommandHandler(
        IPatientProfileRepository patientProfileRepository,
        IPatientMedicalRecordRepository medicalRecordRepository,
        IPatientCareNeedRepository patientCareNeedRepository,
        IPatientCareAssignmentRepository patientCareAssignmentRepository,
        ICareFacilityRepository careFacilityRepository,
        IPatientHealthProgressionBatchSetRepository batchSetRepository,
        IPatientCareAllocator careAllocator,
        IHealthcareSimulationDeletionRepository deletionRepository,
        IPatientHealthOutcomeOutboxWriter outcomeOutboxWriter,
        ICareDeliveryActivityOutboxWriter careDeliveryActivityOutboxWriter,
        PatientIllnessProgressionPolicy progressionPolicy,
        PatientCareNeedAssessmentPolicy careNeedAssessmentPolicy,
        PatientCareDeliveryService careDeliveryService,
        ICareOperationalProfileProvider careOperationalProfileProvider,
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

            PatientHealthProgressionBatchSet? batchSet = await batchSetRepository.GetAsync(
                batch.SimulationHostId,
                batch.SourceRevision,
                cancellationToken);
            PatientHealthProgressionBatchRegistrationStatus registration;
            if (batchSet is null)
            {
                batchSet = PatientHealthProgressionBatchSet.Start(
                    simulationHostId: batch.SimulationHostId,
                    sourceRevision: batch.SourceRevision,
                    correlationId: batch.CorrelationId,
                    totalBatches: batch.TotalBatches,
                    batchNumber: batch.BatchNumber,
                    currentDate: batch.CurrentDate,
                    receivedAtUtc: batch.ObservedAtUtc);
                await batchSetRepository.AddAsync(batchSet, cancellationToken);
                registration = batchSet.IsComplete
                    ? PatientHealthProgressionBatchRegistrationStatus.Completed
                    : PatientHealthProgressionBatchRegistrationStatus.Accepted;
            }
            else
            {
                registration = batchSet.RegisterBatch(
                    correlationId: batch.CorrelationId,
                    totalBatches: batch.TotalBatches,
                    batchNumber: batch.BatchNumber,
                    currentDate: batch.CurrentDate,
                    receivedAtUtc: batch.ObservedAtUtc);
            }

            if (registration == PatientHealthProgressionBatchRegistrationStatus.Duplicate)
                return new AdvancePatientHealthResult(
                    AdvancePatientHealthStatus.Applied,
                    ProcessedPatients: 0,
                    IgnoredPatients: 0,
                    StalePatients: 0,
                    Outcomes: Array.Empty<PatientHealthProgressionResultItem>(),
                    IsBatchSetComplete: batchSet.IsComplete,
                    CompletedBatchSetNow: false);

            IReadOnlyList<PatientProfile> profiles = await patientProfileRepository.GetByIdsAsync(
                batch.PatientIds,
                cancellationToken);
            IReadOnlyList<PatientMedicalRecord> records = await medicalRecordRepository.GetByIdsAsync(
                batch.PatientIds,
                cancellationToken);
            IReadOnlyList<PatientCareNeed> careNeeds = await patientCareNeedRepository.GetByPatientIdsAsync(
                batch.PatientIds,
                cancellationToken);
            IReadOnlyList<PatientCareAssignment> careAssignments =
                await patientCareAssignmentRepository.GetDueScheduledByPatientIdsAsync(
                    batch.SimulationHostId,
                    batch.PatientIds,
                    batch.CurrentDate,
                    cancellationToken);
            IReadOnlyList<CareFacility> careFacilities = careAssignments.Count == 0
                ? []
                : await careFacilityRepository.GetByIdsAsync(
                    careAssignments
                       .Select(assignment => assignment.CareFacilityId)
                       .Distinct()
                       .ToArray(),
                    cancellationToken);
            CareOperationalProfile careOperationalProfile = careAssignments.Count == 0
                ? CareOperationalProfile.Baseline
                : await careOperationalProfileProvider.GetAsync(
                    batch.SimulationHostId,
                    cancellationToken);
            Dictionary<PatientId, PatientProfile> profilesById = profiles.ToDictionary(
                profile => profile.PatientId);
            Dictionary<PatientId, PatientMedicalRecord> recordsById = records.ToDictionary(
                record => record.PatientId);
            Dictionary<PatientId, PatientCareNeed> careNeedsByPatientId = careNeeds.ToDictionary(
                careNeed => careNeed.PatientId);
            Dictionary<PatientId, PatientCareAssignment> careAssignmentsByPatientId =
                careAssignments.ToDictionary(assignment => assignment.PatientId);
            Dictionary<CareFacilityId, CareFacility> careFacilitiesById = careFacilities.ToDictionary(
                facility => facility.CareFacilityId);
            var addedCareNeeds = new List<PatientCareNeed>();
            var outcomes = new List<PatientHealthProgressionResultItem>();
            int processedPatients = 0;
            int ignoredPatients = 0;
            int stalePatients = 0;
            int careAssignmentsDelivered = 0;
            int careAssignmentsCancelled = 0;
            int routineCareDeliveries = 0;
            int urgentCareDeliveries = 0;
            int acuteCareDeliveries = 0;
            int emergencyCareDeliveries = 0;

            foreach (PreparedPatientHealthRisk patient in batch.Patients)
            {
                careAssignmentsByPatientId.TryGetValue(
                    patient.PatientId,
                    out PatientCareAssignment? careAssignment);

                if (!profilesById.TryGetValue(patient.PatientId, out PatientProfile? profile)
                    || !recordsById.TryGetValue(patient.PatientId, out PatientMedicalRecord? record))
                {
                    ignoredPatients++;
                    continue;
                }

                EnsureSameSimulationHost(batch.SimulationHostId, profile, record);

                if (!profile.IsEligibleForCare)
                {
                    if (TryCancelAssignment(
                            careAssignment,
                            batch.CurrentDate,
                            batch.ObservedAtUtc,
                            PatientCareAssignmentCancellationReason.PatientIneligible))
                        careAssignmentsCancelled++;

                    ignoredPatients++;
                    continue;
                }

                if (patient.LifecycleRevision != profile.LastLifecycleRevision
                    || patient.LifecycleRevision != record.LastLifecycleRevision)
                {
                    if (TryCancelAssignment(
                            careAssignment,
                            batch.CurrentDate,
                            batch.ObservedAtUtc,
                            PatientCareAssignmentCancellationReason.PatientLifecycleChanged))
                        careAssignmentsCancelled++;

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

                PatientCareTreatmentOutcome? treatmentOutcome = null;
                if (careAssignment is not null)
                {
                    careNeedsByPatientId.TryGetValue(
                        patient.PatientId,
                        out PatientCareNeed? careNeedForDelivery);
                    careFacilitiesById.TryGetValue(
                        careAssignment.CareFacilityId,
                        out CareFacility? careFacility);
                    PatientCareDeliveryResult delivery = careDeliveryService.Deliver(
                        careAssignment,
                        batch.SimulationHostId,
                        patient.LifecycleRevision,
                        record,
                        careNeedForDelivery,
                        careFacility,
                        batch.CurrentDate,
                        batch.ObservedAtUtc,
                        careOperationalProfile);
                    treatmentOutcome = delivery.TreatmentOutcome;
                    if (delivery.Delivered)
                    {
                        careAssignmentsDelivered++;
                        switch (careAssignment.Urgency)
                        {
                            case CareNeedUrgency.Routine:
                                routineCareDeliveries++;
                                break;
                            case CareNeedUrgency.Urgent:
                                urgentCareDeliveries++;
                                break;
                            case CareNeedUrgency.Acute:
                                acuteCareDeliveries++;
                                break;
                            case CareNeedUrgency.Emergency:
                                emergencyCareDeliveries++;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(careAssignment));
                        }
                    }
                    else if (delivery.Cancelled)
                        careAssignmentsCancelled++;
                }

                if (outcome.HasAnyEffect || treatmentOutcome?.HasAnyEffect == true)
                    outcomes.Add(MapOutcome(
                        record,
                        outcome,
                        treatmentOutcome,
                        patient.LifecycleRevision));

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

            batchSet.RecordCareDeliveryBatch(
                processedPatientCount: processedPatients,
                routineCareDeliveryCount: routineCareDeliveries,
                urgentCareDeliveryCount: urgentCareDeliveries,
                acuteCareDeliveryCount: acuteCareDeliveries,
                emergencyCareDeliveryCount: emergencyCareDeliveries);

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
            }

            if (registration == PatientHealthProgressionBatchRegistrationStatus.Completed)
            {
                if (batchSet.RecordedCareDeliveryBatchCount != batchSet.TotalBatches)
                    throw new InvalidOperationException(
                        "A completed progression batch set must contain care delivery activity for every batch.");

                await careDeliveryActivityOutboxWriter.AddAsync(
                    new CareDeliveryActivitySnapshot(
                        SimulationHostId: batch.SimulationHostId.Value,
                        SourceRevision: batchSet.SourceRevision,
                        CareDate: batchSet.CurrentDate,
                        ProcessedPatientCount: batchSet.ProcessedPatientCount,
                        RoutineCareDeliveryCount: batchSet.RoutineCareDeliveryCount,
                        UrgentCareDeliveryCount: batchSet.UrgentCareDeliveryCount,
                        AcuteCareDeliveryCount: batchSet.AcuteCareDeliveryCount,
                        EmergencyCareDeliveryCount: batchSet.EmergencyCareDeliveryCount,
                        OccurredAtUtc: batch.ObservedAtUtc,
                        CorrelationId: $"{batchSet.CorrelationId}:care-delivery"),
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            int careAssignmentsCreated = 0;
            if (registration == PatientHealthProgressionBatchRegistrationStatus.Completed)
            {
                careAssignmentsCreated = await careAllocator.AllocateAsync(
                    batch.SimulationHostId,
                    batch.CurrentDate.AddDays(1),
                    batch.ObservedAtUtc,
                    cancellationToken);
                if (careAssignmentsCreated > 0)
                    await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new AdvancePatientHealthResult(
                AdvancePatientHealthStatus.Applied,
                ProcessedPatients: processedPatients,
                IgnoredPatients: ignoredPatients,
                StalePatients: stalePatients,
                Outcomes: outcomes,
                IsBatchSetComplete: batchSet.IsComplete,
                CompletedBatchSetNow:
                    registration == PatientHealthProgressionBatchRegistrationStatus.Completed,
                CareAssignmentsCreated: careAssignmentsCreated,
                CareAssignmentsDelivered: careAssignmentsDelivered,
                CareAssignmentsCancelled: careAssignmentsCancelled);
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
            PatientCareTreatmentOutcome? treatmentOutcome,
            long lifecycleRevision)
        {
            return new PatientHealthProgressionResultItem(
                PatientId: record.PatientId.Value,
                HealthScore: record.Health.Value,
                CurrentIllnessKind: record.Illness.CurrentKind,
                CurrentIllnessSeverity: record.Illness.CurrentSeverity,
                DiagnosedOn: record.Illness.DiagnosedOn,
                LastRecoveredOn: record.Illness.LastRecoveredOn,
                HealthDelta: checked(outcome.HealthDelta + (treatmentOutcome?.HealthDelta ?? 0)),
                HappinessDelta: outcome.HappinessDelta,
                EnergyDelta: outcome.EnergyDelta,
                StressDelta: outcome.StressDelta,
                BecameCritical: outcome.BecameCritical,
                LifecycleRevision: lifecycleRevision);
        }

        private static bool TryCancelAssignment(
            PatientCareAssignment? assignment,
            DateOnly cancelledOn,
            DateTimeOffset cancelledAtUtc,
            PatientCareAssignmentCancellationReason reason)
        {
            return assignment?.TryCancel(
                cancelledOn,
                cancelledAtUtc,
                reason) == true;
        }

    }
}
