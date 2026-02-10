namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed record ApplyCityWeatherImpactResult(
        ApplyCityWeatherImpactStatus Status,
        int AffectedPeopleCount);
}
