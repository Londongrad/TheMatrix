namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed record SetCityEmergencyRationingResult(
        SetCityEmergencyRationingStatus Status,
        Guid CityId,
        bool EmergencyRationingEnabled,
        decimal SupplyStressIndex);
}
