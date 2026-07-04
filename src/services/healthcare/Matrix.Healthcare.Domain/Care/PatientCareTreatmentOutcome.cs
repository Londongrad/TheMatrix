namespace Matrix.Healthcare.Domain.Care;

public sealed record PatientCareTreatmentOutcome(
    bool MedicalStateChanged,
    int HealthDelta)
{
    public bool HasAnyEffect => MedicalStateChanged || HealthDelta != 0;
}
