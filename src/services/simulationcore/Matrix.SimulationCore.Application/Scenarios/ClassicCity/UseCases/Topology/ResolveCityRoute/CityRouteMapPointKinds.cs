namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public static class CityRouteMapPointKinds
    {
        public const string RoadNode = "RoadNode";
        public const string ResidentialBuilding = "ResidentialBuilding";
        public const string CityAnchor = "CityAnchor";

        public static bool IsSupported(string value)
        {
            return string.Equals(value, RoadNode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, ResidentialBuilding, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, CityAnchor, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            return value.Replace("-", string.Empty, StringComparison.Ordinal)
               .Replace("_", string.Empty, StringComparison.Ordinal)
               .Trim()
               .ToLowerInvariant() switch
            {
                "roadnode" => RoadNode,
                "residentialbuilding" => ResidentialBuilding,
                "cityanchor" => CityAnchor,
                _ => value.Trim()
            };
        }
    }
}
