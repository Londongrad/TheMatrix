using Matrix.Healthcare.Domain.Operations;

namespace Matrix.Healthcare.Application.Operations;

public sealed record PopulationHealthSnapshot(
    Guid SimulationHostId,
    long SourceRevision,
    DateOnly CurrentDate,
    CareSystemPressureProfile Pressure,
    IReadOnlyList<CommunityHealthSnapshot> Communities,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId);
