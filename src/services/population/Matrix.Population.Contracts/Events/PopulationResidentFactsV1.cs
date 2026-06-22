namespace Matrix.Population.Contracts.Events
{
    /// <summary>
    ///     Scenario-neutral demographic facts owned by Population and replicated by downstream contexts.
    /// </summary>
    public sealed record PopulationResidentFactsV1(
        Guid ResidentId,
        DateOnly BirthDate,
        string Sex,
        bool IsAlive,
        bool IsActive);
}
