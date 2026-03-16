using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class ClassicCityHouseholdAccountSyncBatchFactory
    {
        public static ClassicCityHouseholdAccountSyncBatchV1[] Build(
            Guid cityId,
            IReadOnlyCollection<Household> households,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements,
            string correlationId,
            DateTimeOffset occurredAtUtc,
            int batchSize)
        {
            var placementsByHouseholdId = placements.ToDictionary(x => x.HouseholdId);
            ClassicCityHouseholdAccountSyncItemV1[] items = households
               .OrderBy(x => x.CreatedAtUtc)
               .ThenBy(x => x.Id.Value)
               .Select(household =>
                {
                    ClassicCityHouseholdPlacement placement = placementsByHouseholdId[household.Id];
                    return new ClassicCityHouseholdAccountSyncItemV1(
                        HouseholdId: household.Id.Value,
                        ExternalReferenceCode: BuildExternalReferenceCode(household.Id),
                        Name: BuildAccountName(household.Id),
                        MemberCount: household.Size.Value,
                        OpeningBalanceAmount: household.CashReserve.Amount,
                        IsHoused: placement.HousingStatus == HousingStatus.Housed,
                        CreatedAtUtc: household.CreatedAtUtc);
                })
               .ToArray();

            if (items.Length == 0)
                return [];

            ClassicCityHouseholdAccountSyncBatchV1[] batches = items
               .Chunk(batchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityHouseholdAccountSyncBatchV1(
                    CityId: cityId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Households: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: occurredAtUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        public static string BuildExternalReferenceCode(HouseholdId householdId)
        {
            return $"classic-city-household:{householdId.Value:N}";
        }

        private static string BuildAccountName(HouseholdId householdId)
        {
            string shortCode = householdId.Value.ToString("N")[..8]
               .ToUpperInvariant();
            return $"Household {shortCode}";
        }
    }
}
