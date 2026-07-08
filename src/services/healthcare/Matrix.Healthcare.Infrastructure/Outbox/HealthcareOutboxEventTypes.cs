namespace Matrix.Healthcare.Infrastructure.Outbox
{
    public static class HealthcareOutboxEventTypes
    {
        public const string PatientHealthOutcomeBatchV1 =
            "healthcare.patient-health-outcome-batch.v1";

        public const string CareDeliveryActivityV1 =
            "healthcare.care-delivery-activity.v1";

        public const string PopulationHealthSnapshotV1 =
            "healthcare.population-health-snapshot.v1";
    }
}
