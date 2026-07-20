namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentHouseholdHealthContextV1(
        double StabilityScore,
        int AdultProviderCount,
        int AdultStructuredParticipantCount,
        int FunctionalLimitationCount,
        bool HasStructuredSupport);
}
