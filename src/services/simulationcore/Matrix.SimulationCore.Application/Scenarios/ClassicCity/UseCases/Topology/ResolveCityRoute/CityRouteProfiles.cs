namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public static class CityRouteProfiles
    {
        public const string Pedestrian = "Pedestrian";
        public const string ServiceVehicle = "ServiceVehicle";
        public const string EmergencyResponse = "EmergencyResponse";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, Pedestrian, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, ServiceVehicle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, EmergencyResponse, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            return value.Replace("-", string.Empty, StringComparison.Ordinal)
               .Replace("_", string.Empty, StringComparison.Ordinal)
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
