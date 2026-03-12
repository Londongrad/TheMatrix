using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Services
{
    public sealed class CityMunicipalOperatingCyclePolicy
    {
        public IReadOnlyList<CityMunicipalOperatingDisbursementDecision> BuildDisbursements(
            CityBudgetAllocation allocation,
            IReadOnlyList<CityBusiness> businesses)
        {
            decimal availableAmount = allocation.GetAvailableAmount().Amount;
            if (availableAmount <= 0m || allocation.Category == CityBudgetCategory.Taxation)
            {
                return Array.Empty<CityMunicipalOperatingDisbursementDecision>();
            }

            CityBusiness[] eligibleBusinesses = businesses
                .Where(x => IsEligible(x.Kind, allocation.Category))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToArray();

            if (eligibleBusinesses.Length == 0)
            {
                return Array.Empty<CityMunicipalOperatingDisbursementDecision>();
            }

            decimal plannedCycleAmount = decimal.Round(allocation.TargetAmount.Amount * 0.10m, 2, MidpointRounding.AwayFromZero);
            if (plannedCycleAmount <= 0m)
            {
                plannedCycleAmount = Math.Min(availableAmount, 0.01m);
            }

            decimal cycleAmount = Math.Min(availableAmount, plannedCycleAmount);
            if (cycleAmount <= 0m)
            {
                return Array.Empty<CityMunicipalOperatingDisbursementDecision>();
            }

            decimal baseAmount = decimal.Round(cycleAmount / eligibleBusinesses.Length, 2, MidpointRounding.AwayFromZero);
            decimal distributedAmount = 0m;

            var decisions = new List<CityMunicipalOperatingDisbursementDecision>(eligibleBusinesses.Length);
            for (int index = 0; index < eligibleBusinesses.Length; index++)
            {
                decimal amount = index == eligibleBusinesses.Length - 1
                    ? decimal.Round(cycleAmount - distributedAmount, 2, MidpointRounding.AwayFromZero)
                    : baseAmount;

                if (amount <= 0m)
                {
                    continue;
                }

                decisions.Add(new CityMunicipalOperatingDisbursementDecision(eligibleBusinesses[index].Id, amount));
                distributedAmount += amount;
            }

            return decisions;
        }

        private static bool IsEligible(CityBusinessKind businessKind, CityBudgetCategory category)
        {
            return category switch
            {
                CityBudgetCategory.General => businessKind is CityBusinessKind.Service or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Operations => businessKind is CityBusinessKind.Service or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Housing => businessKind is CityBusinessKind.Landlord or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Commerce => businessKind is CityBusinessKind.RetailStore or CityBusinessKind.Service or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Infrastructure => businessKind is CityBusinessKind.Utility or CityBusinessKind.Manufacturer or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Healthcare => businessKind is CityBusinessKind.Service or CityBusinessKind.MunicipalVendor,
                CityBudgetCategory.Education => businessKind is CityBusinessKind.Service or CityBusinessKind.MunicipalVendor,
                _ => false
            };
        }
    }
}
