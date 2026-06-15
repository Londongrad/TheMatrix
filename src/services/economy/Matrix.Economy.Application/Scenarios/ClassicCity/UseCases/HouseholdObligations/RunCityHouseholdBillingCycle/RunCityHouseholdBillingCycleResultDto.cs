namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed record RunCityHouseholdBillingCycleResultDto(
        Guid CityId,
        string AsOfUtc,
        int ChargedObligations,
        decimal TotalChargedAmount,
        decimal TotalTaxAmount);
}
