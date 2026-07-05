using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Care.DeliverPatientCare;

public sealed class PatientCareDeliveryService(
    PatientCareTreatmentPolicy treatmentPolicy)
{
    public PatientCareDeliveryResult Deliver(
        PatientCareAssignment assignment,
        SimulationHostId simulationHostId,
        long patientLifecycleRevision,
        PatientMedicalRecord medicalRecord,
        PatientCareNeed? careNeed,
        CareFacility? facility,
        DateOnly currentDate,
        DateTimeOffset deliveredAtUtc,
        CareOperationalProfile? operationalProfile = null)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(medicalRecord);
        EnsureIdentity(assignment, medicalRecord, simulationHostId);

        if (assignment.Status != PatientCareAssignmentStatus.Scheduled)
            return new PatientCareDeliveryResult(assignment.Status, TreatmentOutcome: null);

        if (assignment.LifecycleRevision != patientLifecycleRevision
            || medicalRecord.LastLifecycleRevision != patientLifecycleRevision)
            return Cancel(
                assignment,
                currentDate,
                deliveredAtUtc,
                PatientCareAssignmentCancellationReason.PatientLifecycleChanged);

        if (careNeed is not null)
            EnsureCareNeedIdentity(careNeed, assignment, simulationHostId);

        if (careNeed is null
            || !careNeed.IsActive
            || careNeed.LastLifecycleRevision != patientLifecycleRevision)
            return Cancel(
                assignment,
                currentDate,
                deliveredAtUtc,
                PatientCareAssignmentCancellationReason.CareNoLongerRequired);

        if (facility is not null)
            EnsureFacilityIdentity(facility, assignment, simulationHostId);

        if (facility is null || !facility.IsActive)
            return Cancel(
                assignment,
                currentDate,
                deliveredAtUtc,
                PatientCareAssignmentCancellationReason.FacilityUnavailable);

        PatientCareTreatmentOutcome treatment = treatmentPolicy.Apply(
            medicalRecord,
            assignment.Urgency,
            currentDate,
            operationalProfile);
        assignment.TryMarkDelivered(
            currentDate,
            deliveredAtUtc,
            treatment.HealthDelta,
            treatment.MedicalStateChanged);

        return new PatientCareDeliveryResult(
            assignment.Status,
            treatment);
    }

    private static PatientCareDeliveryResult Cancel(
        PatientCareAssignment assignment,
        DateOnly currentDate,
        DateTimeOffset cancelledAtUtc,
        PatientCareAssignmentCancellationReason reason)
    {
        assignment.TryCancel(
            currentDate,
            cancelledAtUtc,
            reason);
        return new PatientCareDeliveryResult(
            assignment.Status,
            TreatmentOutcome: null);
    }

    private static void EnsureIdentity(
        PatientCareAssignment assignment,
        PatientMedicalRecord medicalRecord,
        SimulationHostId simulationHostId)
    {
        if (assignment.SimulationHostId != simulationHostId
            || medicalRecord.SimulationHostId != simulationHostId
            || assignment.PatientId != medicalRecord.PatientId)
            throw new InvalidOperationException(
                "Patient care delivery cannot cross patient or simulation host boundaries.");
    }

    private static void EnsureCareNeedIdentity(
        PatientCareNeed careNeed,
        PatientCareAssignment assignment,
        SimulationHostId simulationHostId)
    {
        if (careNeed.SimulationHostId != simulationHostId
            || careNeed.PatientId != assignment.PatientId)
            throw new InvalidOperationException(
                "Patient care delivery cannot use a care need from another patient or simulation host.");
    }

    private static void EnsureFacilityIdentity(
        CareFacility facility,
        PatientCareAssignment assignment,
        SimulationHostId simulationHostId)
    {
        if (facility.SimulationHostId != simulationHostId
            || facility.CareFacilityId != assignment.CareFacilityId)
            throw new InvalidOperationException(
                "Patient care delivery cannot use another assignment's facility.");
    }
}
