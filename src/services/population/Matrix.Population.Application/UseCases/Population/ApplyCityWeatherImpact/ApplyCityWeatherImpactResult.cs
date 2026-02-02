namespace Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed record ApplyCityWeatherImpactResult(
        ApplyCityWeatherImpactStatus Status,
        int AffectedPeopleCount);
}
