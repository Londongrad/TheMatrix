namespace Matrix.Healthcare.Domain.Operations;

public sealed record CareSystemPressureProfile(
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount,
    decimal MedicalLoadIndex,
    decimal TriagePressureIndex,
    decimal RecoverySupportIndex);
