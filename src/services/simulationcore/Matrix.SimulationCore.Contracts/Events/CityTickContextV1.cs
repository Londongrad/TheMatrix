namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityTickContextV1(
        Guid SimulationId,
        Guid CityId,
        string SimulationKind,
        long TickId,
        DateTimeOffset EffectiveSimTimeUtc,
        CityTickPhaseV1 Phase,
        int ModelVersion,
        string CausationId,
        string CorrelationId);
}
