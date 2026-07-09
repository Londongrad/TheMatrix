namespace Matrix.Healthcare.Domain.Patients;

public sealed record PatientCommunityHealthBurden(
    PatientCommunityId CommunityId,
    PatientPopulationHealthBurden Burden)
{
    public PatientPopulationHealthBurden Burden { get; } =
        Burden ?? throw new ArgumentNullException(nameof(Burden));
}
