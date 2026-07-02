namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationCareFacilityProvisioningV1(
    Guid FacilityId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int DailyPatientCapacity,
    bool IsActive);
