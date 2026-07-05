namespace Matrix.Healthcare.Domain.Operations;

public sealed record CareOperationalProfile(
    CareQualityMultiplier ServiceQuality,
    CareAvailabilityIndex MedicineAvailability,
    CareAvailabilityIndex MedicineShortageRisk)
{
    public static CareOperationalProfile Baseline => new(
        CareQualityMultiplier.Baseline,
        CareAvailabilityIndex.Full,
        CareAvailabilityIndex.None);

    public decimal TreatmentEffectivenessMultiplier
    {
        get
        {
            decimal supplyEffect = 0.25m + (MedicineAvailability.Value * 0.75m);
            decimal shortageEffect = 1m - (MedicineShortageRisk.Value * 0.50m);
            return decimal.Round(
                Math.Clamp(
                    ServiceQuality.Value * supplyEffect * shortageEffect,
                    CareQualityMultiplier.Minimum,
                    CareQualityMultiplier.Maximum),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
