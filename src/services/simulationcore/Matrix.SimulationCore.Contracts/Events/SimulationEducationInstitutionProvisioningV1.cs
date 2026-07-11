namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationEducationInstitutionProvisioningV1(
    Guid InstitutionId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int Capacity,
    bool IsActive);
