namespace Matrix.Resources.Application.Scenarios.ClassicCity
{
    public static class ClassicCityScenario
    {
        public const string Name = "ClassicCity";

        public static bool IsMatch(string simulationKind)
        {
            return string.Equals(
                a: simulationKind,
                b: Name,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }
    }
}
