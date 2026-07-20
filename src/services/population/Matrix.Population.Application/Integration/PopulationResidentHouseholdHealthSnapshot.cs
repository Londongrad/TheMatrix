namespace Matrix.Population.Application.Integration
{
    public sealed record PopulationResidentHouseholdHealthSnapshot(
        double StabilityScore,
        int AdultProviderCount,
        int AdultStructuredParticipantCount,
        int FunctionalLimitationCount,
        bool HasStructuredSupport);
}
