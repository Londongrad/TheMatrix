namespace Matrix.SimulationCore.Application.Abstractions.Outbox;

public sealed record EducationInstitutionProvisioning(
    Guid InstitutionId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int Capacity,
    bool IsActive);
