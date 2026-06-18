using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    internal static class ClassicCityEconomySettlementBatchFactory
    {
        private const int HouseholdCashflowBatchSize = 500;
        private const int WorkplacePayrollBatchSize = 500;

        public static ClassicCityHouseholdCashflowSettlementBatchV1[] BuildHouseholdCashflowSettlementBatches(
            Guid cityId,
            DateOnly currentDate,
            int settledDays,
            IReadOnlyCollection<ClassicCityHouseholdCashflowSettlementItemV1> items,
            string correlationId,
            DateTimeOffset occurredAtUtc)
        {
            if (items.Count == 0 || settledDays <= 0)
                return [];

            ClassicCityHouseholdCashflowSettlementBatchV1[] batches = items
               .Chunk(HouseholdCashflowBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityHouseholdCashflowSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: currentDate,
                    SettledDays: settledDays,
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

        public static ClassicCityWorkplacePayrollSettlementBatchV1[] BuildWorkplacePayrollSettlementBatches(
            Guid cityId,
            DateOnly currentDate,
            int settledDays,
            IReadOnlyCollection<ClassicCityWorkplacePayrollSettlementItemV1> items,
            string correlationId,
            DateTimeOffset occurredAtUtc)
        {
            if (items.Count == 0 || settledDays <= 0)
                return [];

            ClassicCityWorkplacePayrollSettlementBatchV1[] batches = items
               .Chunk(WorkplacePayrollBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityWorkplacePayrollSettlementBatchV1(
                    CityId: cityId,
                    CurrentDate: currentDate,
                    SettledDays: settledDays,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Payrolls: chunk,
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

        public static string BuildHouseholdExternalReferenceCode(HouseholdId householdId)
        {
            return $"classic-city-household:{householdId.Value:N}";
        }
    }
}
