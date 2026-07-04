namespace Matrix.Healthcare.Domain.Care;

public enum PatientCareAssignmentCancellationReason
{
    PatientDataUnavailable = 0,
    PatientLifecycleChanged = 1,
    PatientIneligible = 2,
    FacilityUnavailable = 3,
    CareNoLongerRequired = 4
}
