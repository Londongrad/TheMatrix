namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Provisioning
{
    public sealed class ProvisioningRecoveryOptions
    {
        public const string SectionName = "SimulationCore:Provisioning";

        public int PollIntervalSeconds { get; set; } = 5;
        public int LeaseDurationSeconds { get; set; } = 180;
        public int MaxBatchSize { get; set; } = 4;
    }
}
