using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Care;

public sealed record PatientCareAllocationDecision(
    PatientId PatientId,
    CareFacilityId CareFacilityId,
    CareNeedUrgency Urgency,
    long AssessmentRevision,
    long LifecycleRevision);
