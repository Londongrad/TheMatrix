namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdCommutePressureProfile(
        int RoutedResidentCount,
        int BlockedRouteCount,
        decimal AccessibilityDeficitIndex,
        decimal TravelFatigueIndex);
}
