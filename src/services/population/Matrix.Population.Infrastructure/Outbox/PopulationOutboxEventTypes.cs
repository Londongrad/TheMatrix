namespace Matrix.Population.Infrastructure.Outbox
{
    public static class PopulationOutboxEventTypes
    {
        public const string PopulationResidentFactsBatchV1 =
            "population.resident-facts-batch.v1";

        public const string PopulationResidentHealthRiskBatchV1 =
            "population.resident-health-risk-batch.v1";

        public const string PopulationResidentHealthRiskBatchV2 =
            "population.resident-health-risk-batch.v2";

        public const string PopulationResidentVitalStateBatchV1 =
            "population.resident-vital-state-batch.v1";
    }
}
