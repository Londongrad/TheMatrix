namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    internal static class ClassicCitySetupSteps
    {
        public const string Scenario = "scenario";
        public const string Profile = "profile";
        public const string Environment = "environment";
        public const string Launch = "launch";

        public static bool IsKnown(string? value)
        {
            return value is Scenario or Profile or Environment or Launch;
        }
    }
}
