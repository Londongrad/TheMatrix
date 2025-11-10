using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Events
{
    public sealed record class IncomeEarned(
        PersonId PersonId,
        HouseholdId HouseholdId,
        DistrictId DistrictId,
        WorkplaceId WorkplaceId,
        MonthlyIncome MonthlyIncome,
        int SimulationMonth);
}
