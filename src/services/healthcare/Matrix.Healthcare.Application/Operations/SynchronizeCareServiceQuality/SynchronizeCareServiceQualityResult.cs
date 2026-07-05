namespace Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;

public sealed record SynchronizeCareServiceQualityResult(
    SynchronizeCareServiceQualityStatus Status,
    bool StateCreated,
    bool StateUpdated);
