namespace Matrix.Economy.Infrastructure.Outbox
{
    public static class EconomyOutboxEventTypes
    {
        public const string ClassicCityOperationalBudgetPressureSnapshotV1 =
            "economy.classic-city-operational-budget-pressure-snapshot.v1";

        public const string ClassicCityCostOfLivingSnapshotV1 =
            "economy.classic-city-cost-of-living-snapshot.v1";

        public const string ClassicCityServiceQualitySnapshotV1 =
            "economy.classic-city-service-quality-snapshot.v1";

        public const string ClassicCityEmployerFinancialStressBatchV1 =
            "economy.classic-city-employer-financial-stress-batch.v1";

        public const string ClassicCityHouseholdFinancialStressBatchV1 =
            "economy.classic-city-household-financial-stress-batch.v1";
    }
}
