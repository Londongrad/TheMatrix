namespace Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests
{
    public sealed record DispatchCityResupplyRequest(
        ResupplyFocus Focus = ResupplyFocus.All,
        ResupplyIntensity Intensity = ResupplyIntensity.Medium,
        bool EmergencyOverride = false);
}
