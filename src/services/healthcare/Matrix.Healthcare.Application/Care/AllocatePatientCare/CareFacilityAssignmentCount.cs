using Matrix.Healthcare.Domain.Facilities;

namespace Matrix.Healthcare.Application.Care.AllocatePatientCare;

public sealed record CareFacilityAssignmentCount(
    CareFacilityId CareFacilityId,
    int AssignedPatients);
