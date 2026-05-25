namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public static class CityRouteMapPointKinds
    {
        public const string RoadNode = "RoadNode";
        public const string ResidentialBuilding = "ResidentialBuilding";
        public const string CityAnchor = "CityAnchor";

        public static bool IsSupported(string value)
        {
            return string.Equals(
                       a: value,
                       b: RoadNode,
                       comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       a: value,
                       b: ResidentialBuilding,
                       comparisonType: StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       a: value,
                       b: CityAnchor,
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
                "roadnode" => RoadNode,
                "residentialbuilding" => ResidentialBuilding,
                "cityanchor" => CityAnchor,
                _ => value.Trim()
            };
        }
    }
}
