namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationCommuteContext(
        bool HasRouteData,
        bool IsAccessible,
        decimal AccessibilityIndex,
        decimal PassabilityIndex,
        decimal? EstimatedTravelTimeMinutes)
    {
        public static CityPopulationCommuteContext Neutral { get; } = new(
            HasRouteData: false,
            IsAccessible: true,
            AccessibilityIndex: 1m,
            PassabilityIndex: 1m,
            EstimatedTravelTimeMinutes: null);

        public static CityPopulationCommuteContext Blocked { get; } = new(
            HasRouteData: true,
            IsAccessible: false,
            AccessibilityIndex: 0m,
            PassabilityIndex: 0m,
            EstimatedTravelTimeMinutes: null);
    }
}
