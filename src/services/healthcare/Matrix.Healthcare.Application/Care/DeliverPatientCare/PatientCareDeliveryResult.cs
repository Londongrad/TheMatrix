using Matrix.Healthcare.Domain.Care;

namespace Matrix.Healthcare.Application.Care.DeliverPatientCare;

public sealed record PatientCareDeliveryResult(
    PatientCareAssignmentStatus Status,
    PatientCareTreatmentOutcome? TreatmentOutcome)
{
    public bool Delivered => Status == PatientCareAssignmentStatus.Delivered;
    public bool Cancelled => Status == PatientCareAssignmentStatus.Cancelled;
}
