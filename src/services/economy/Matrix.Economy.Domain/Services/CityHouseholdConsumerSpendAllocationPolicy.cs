using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Services
{
    public sealed class CityHouseholdConsumerSpendAllocationPolicy
    {
        public IReadOnlyList<CityHouseholdConsumerSpendAllocation> Allocate(
            Guid householdId,
            DateOnly currentDate,
            Money retailTurnover,
            Money totalSalesTax,
            Money retailStoreSpend,
            Money serviceSpend,
            Money municipalSpend,
            IReadOnlyList<CityBusiness> businesses)
        {
            ArgumentNullException.ThrowIfNull(businesses);

            if (!retailTurnover.IsPositive || businesses.Count == 0)
                return [];

            List<SpendSegment> segments = BuildSegments(
                retailTurnover: retailTurnover,
                retailStoreSpend: retailStoreSpend,
                serviceSpend: serviceSpend,
                municipalSpend: municipalSpend);

            if (segments.Count == 0)
                return [];

            AllocateSalesTax(
                segments: segments,
                totalSalesTax: totalSalesTax);

            var allocations = new List<CityHouseholdConsumerSpendAllocation>(segments.Count);

            foreach (SpendSegment segment in segments)
            {
                CityBusiness? business = ResolveBusiness(
                    householdId: householdId,
                    currentDate: currentDate,
                    segment: segment,
                    businesses: businesses);

                if (business is null)
                    continue;

                allocations.Add(
                    new CityHouseholdConsumerSpendAllocation(
                        Business: business,
                        GrossAmount: segment.GrossAmount,
                        SalesTaxAmount: segment.SalesTaxAmount,
                        SegmentKey: segment.Key,
                        Title: segment.Title,
                        Description: segment.Description));
            }

            return allocations;
        }

        private static List<SpendSegment> BuildSegments(
            Money retailTurnover,
            Money retailStoreSpend,
            Money serviceSpend,
            Money municipalSpend)
        {
            decimal retailStoreAmount = decimal.Max(0m, retailStoreSpend.Amount);
            decimal serviceAmount = decimal.Max(0m, serviceSpend.Amount);
            decimal municipalAmount = decimal.Max(0m, municipalSpend.Amount);
            decimal totalCategorizedAmount = retailStoreAmount + serviceAmount + municipalAmount;

            if (totalCategorizedAmount <= 0m)
                retailStoreAmount = retailTurnover.Amount;
            else
            {
                decimal remainder = decimal.Round(
                    d: retailTurnover.Amount - totalCategorizedAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero);

                if (remainder != 0m)
                {
                    if (retailStoreAmount > 0m)
                        retailStoreAmount = decimal.Max(0m, retailStoreAmount + remainder);
                    else if (serviceAmount > 0m)
                        serviceAmount = decimal.Max(0m, serviceAmount + remainder);
                    else
                        municipalAmount = decimal.Max(0m, municipalAmount + remainder);
                }
            }

            var segments = new List<SpendSegment>(3);

            if (retailStoreAmount > 0m)
                segments.Add(
                    new SpendSegment(
                        key: "retail-store",
                        grossAmount: Money.FromDecimal(retailStoreAmount),
                        salesTaxAmount: Money.Zero,
                        title: "Household retail basket settlement",
                        description: "Household essentials and goods spending settled from classic city cashflow."));

            if (serviceAmount > 0m)
                segments.Add(
                    new SpendSegment(
                        key: "service",
                        grossAmount: Money.FromDecimal(serviceAmount),
                        salesTaxAmount: Money.Zero,
                        title: "Household service settlement",
                        description: "Household service spending settled from classic city cashflow."));

            if (municipalAmount > 0m)
                segments.Add(
                    new SpendSegment(
                        key: "municipal",
                        grossAmount: Money.FromDecimal(municipalAmount),
                        salesTaxAmount: Money.Zero,
                        title: "Household civic settlement",
                        description: "Household civic and municipal spending settled from classic city cashflow."));

            return segments;
        }

        private static void AllocateSalesTax(
            IReadOnlyList<SpendSegment> segments,
            Money totalSalesTax)
        {
            if (segments.Count == 0 || !totalSalesTax.IsPositive)
                return;

            decimal remainingTaxAmount = totalSalesTax.Amount;
            decimal totalGrossAmount = segments.Sum(x => x.GrossAmount.Amount);

            for (int index = 0; index < segments.Count; index++)
            {
                SpendSegment segment = segments[index];
                decimal allocatedTaxAmount = index == segments.Count - 1
                    ? remainingTaxAmount
                    : decimal.Round(
                        d: totalSalesTax.Amount * (segment.GrossAmount.Amount / totalGrossAmount),
                        decimals: 2,
                        mode: MidpointRounding.AwayFromZero);

                segment.SalesTaxAmount = Money.FromDecimal(allocatedTaxAmount);
                remainingTaxAmount = decimal.Round(
                    d: remainingTaxAmount - allocatedTaxAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero);
            }
        }

        private static CityBusiness? ResolveBusiness(
            Guid householdId,
            DateOnly currentDate,
            SpendSegment segment,
            IReadOnlyList<CityBusiness> businesses)
        {
            IReadOnlyList<CityBusiness> candidates = ResolveCandidates(
                segmentKey: segment.Key,
                businesses: businesses);

            if (candidates.Count == 0)
                return null;

            int index = GetStableIndex(
                householdId: householdId,
                currentDate: currentDate,
                salt: segment.Key switch
                {
                    "retail-store" => 211,
                    "service" => 367,
                    "municipal" => 593,
                    _ => 101
                },
                modulus: candidates.Count);

            return candidates[index];
        }

        private static IReadOnlyList<CityBusiness> ResolveCandidates(
            string segmentKey,
            IReadOnlyList<CityBusiness> businesses)
        {
            CityBusinessKind[][] priorities = segmentKey switch
            {
                "retail-store" =>
                [
                    [CityBusinessKind.RetailStore],
                    [CityBusinessKind.Service],
                    [CityBusinessKind.MunicipalVendor],
                    [CityBusinessKind.Generic],
                    [CityBusinessKind.Utility]
                ],
                "service" =>
                [
                    [CityBusinessKind.Service],
                    [CityBusinessKind.RetailStore],
                    [CityBusinessKind.MunicipalVendor],
                    [CityBusinessKind.Generic],
                    [CityBusinessKind.Utility]
                ],
                "municipal" =>
                [
                    [CityBusinessKind.MunicipalVendor],
                    [CityBusinessKind.Service],
                    [CityBusinessKind.RetailStore],
                    [CityBusinessKind.Generic],
                    [CityBusinessKind.Utility]
                ],
                _ =>
                [
                    [CityBusinessKind.RetailStore],
                    [CityBusinessKind.Service],
                    [CityBusinessKind.MunicipalVendor],
                    [CityBusinessKind.Generic]
                ]
            };

            foreach (CityBusinessKind[] priority in priorities)
            {
                CityBusiness[] candidates = businesses
                   .Where(x => priority.Contains(x.Kind))
                   .OrderBy(x => x.Name)
                   .ThenBy(x => x.Id)
                   .ToArray();

                if (candidates.Length > 0)
                    return candidates;
            }

            return [];
        }

        private static int GetStableIndex(
            Guid householdId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = householdId.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + (currentDate.DayNumber / 30);
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private sealed class SpendSegment(
            string key,
            Money grossAmount,
            Money salesTaxAmount,
            string title,
            string description)
        {
            public string Key { get; } = key;
            public Money GrossAmount { get; } = grossAmount;
            public Money SalesTaxAmount { get; set; } = salesTaxAmount;
            public string Title { get; } = title;
            public string Description { get; } = description;
        }
    }
}
