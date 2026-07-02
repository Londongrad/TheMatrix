namespace Matrix.SimulationCore.Application.Abstractions.Outbox;

public sealed record CareFacilityProvisioning(
    Guid FacilityId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int DailyPatientCapacity,
    bool IsActive);
