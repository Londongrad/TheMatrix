using Matrix.Healthcare.Domain.Operations;

namespace Matrix.Healthcare.Application.Operations;

public sealed record PopulationHealthSnapshot(
    Guid SimulationHostId,
    long SourceRevision,
    DateOnly CurrentDate,
    CareSystemPressureProfile Pressure,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId);
