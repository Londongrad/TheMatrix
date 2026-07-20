namespace Matrix.Healthcare.Domain.Progression
{
    public sealed record PatientHouseholdHealthContext(
        double StabilityScore,
        int AdultProviderCount,
        int AdultStructuredParticipantCount,
        int FunctionalLimitationCount,
        bool HasStructuredSupport);
}
