namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationHealthcarePressureProfile(
        int ActiveIllnessCount,
        int SevereIllnessCount,
        decimal MedicalLoadIndex,
        decimal TriagePressureIndex,
        decimal RecoverySupportIndex)
    {
        public static CityPopulationHealthcarePressureProfile Baseline =>
            new(0, 0, 0.20m, 0m, 1m);
    }
}
