namespace Matrix.Population.Infrastructure.Outbox
{
    public static class PopulationOutboxEventTypes
    {
        public const string PopulationResidentFactsBatchV1 =
            "population.resident-facts-batch.v1";

        public const string PopulationResidentMedicalStateBatchV1 =
            "population.resident-medical-state-batch.v1";
    }
}
