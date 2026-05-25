namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public static class CityRouteProfiles
    {
        public const string Pedestrian = "Pedestrian";
        public const string ServiceVehicle = "ServiceVehicle";
        public const string EmergencyResponse = "EmergencyResponse";

        public static bool IsSupported(string value)
        {
            return string.Equals(
                       a: value,
                       b: Pedestrian,
                       comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       a: value,
                       b: ServiceVehicle,
                       comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       a: value,
                       b: EmergencyResponse,
                       comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            return value.Replace(
                        oldValue: "-",
                        newValue: string.Empty,
                        comparisonType: StringComparison.Ordinal)
                   .Replace(
                        oldValue: "_",
                        newValue: string.Empty,
                        comparisonType: StringComparison.Ordinal)
                   .Trim()
                   .ToLowerInvariant() switch
                {
                    "pedestrian" => Pedestrian,
                    "servicevehicle" => ServiceVehicle,
                    "emergencyresponse" => EmergencyResponse,
                    _ => value.Trim()
                };
        }
    }
}
