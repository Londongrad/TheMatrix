namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;

public static class ClassicCityRuntimeKeys
{
    public const string ScenarioKey = "classic-city";
    public const string HostTypeKey = "city";

    public static bool IsMatch(string scenarioKey, string hostTypeKey)
    {
        return string.Equals(
                   scenarioKey,
                   ScenarioKey,
                   StringComparison.Ordinal) &&
               string.Equals(
                   hostTypeKey,
                   HostTypeKey,
                   StringComparison.Ordinal);
    }
}
