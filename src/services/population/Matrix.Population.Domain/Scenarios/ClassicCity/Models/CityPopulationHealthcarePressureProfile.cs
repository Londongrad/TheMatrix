namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityPopulationHealthcarePressureProfile(
        int ActiveIllnessCount,
        int SevereIllnessCount,
        decimal MedicalLoadIndex,
        decimal TriagePressureIndex,
        decimal RecoverySupportIndex);
}
