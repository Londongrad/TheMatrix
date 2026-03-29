using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Services
{
    public sealed record CityStockpileBudgetDecision(
        bool Blocked,
        ResupplyIntensity RequestedIntensity,
        ResupplyIntensity AppliedIntensity,
        decimal PressureIndex,
        string AuthorizationLevel,
        decimal AvailableAmount);
}
