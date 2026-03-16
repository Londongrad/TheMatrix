using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Services
{
    public sealed class CityBudgetOperatingExpensePolicy
    {
        public CityBudgetOperatingExpenseProfile Build(CityBudgetSettlement settlement)
        {
            var residentServicesExpense = Money.FromDecimal(settlement.ResidentCount * settlement.SettledDays * 1.25m);
            var householdInfrastructureExpense =
                Money.FromDecimal(settlement.HouseholdCount * settlement.SettledDays * 0.55m);
            Money housingSupportExpense = settlement.HousingSpend.Multiply(0.04m);
            Money commerceSupportExpense = settlement.RetailTurnover.Multiply(0.015m);

            Money totalExpense = residentServicesExpense
               .Add(householdInfrastructureExpense)
               .Add(housingSupportExpense)
               .Add(commerceSupportExpense);

            return new CityBudgetOperatingExpenseProfile(totalExpense);
        }
    }
}
