namespace Matrix.Economy.Contracts.HouseholdObligations.Requests
{
    public sealed class RunCityHouseholdBillingCycleRequest
    {
        public DateTimeOffset? AsOfUtc { get; set; }
    }
}
