namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public static class ClassicCityOutboxEventTypes
    {
        public const string ResidentActivityConditionsBatchV1 = "population.classic-city-resident-activity-conditions-batch.v1";
        public const string CityEconomyDailySettlementV1 = "population.city-economy-daily-settlement.v1";

        public const string ClassicCityHouseholdAccountSyncBatchV1 =
            "population.classic-city-household-account-sync-batch.v1";

        public const string ClassicCityWorkplaceBusinessSyncBatchV1 =
            "population.classic-city-workplace-business-sync-batch.v1";

        public const string ClassicCityWorkplacePayrollSettlementBatchV1 =
            "population.classic-city-workplace-payroll-settlement-batch.v1";

        public const string ClassicCityHouseholdCashflowSettlementBatchV1 =
            "population.classic-city-household-cashflow-settlement-batch.v1";
    }
}
