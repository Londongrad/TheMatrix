using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed record RunCityHouseholdBillingCycleCommand(
        Guid CityId,
        DateTimeOffset? AsOfUtc) : IRequest<RunCityHouseholdBillingCycleResultDto>;
}
