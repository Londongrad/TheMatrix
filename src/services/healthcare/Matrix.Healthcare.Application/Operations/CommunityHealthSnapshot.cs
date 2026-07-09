namespace Matrix.Healthcare.Application.Operations;

public sealed record CommunityHealthSnapshot(
    Guid CommunityId,
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount);
