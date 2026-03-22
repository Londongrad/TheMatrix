namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions
{
    public sealed record CityWeatherSystemInput(
        string Type,
        string Severity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa);
}
