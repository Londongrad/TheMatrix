namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CitySystemsResourceDemandSnapshot(
        decimal FuelDemandPressureIndex,
        decimal SparePartsDemandPressureIndex,
        decimal FiltersDemandPressureIndex,
        decimal EmergencyWaterDemandPressureIndex,
        decimal OverallDemandPressureIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc)
    {
        public static CitySystemsResourceDemandSnapshot Neutral(
            DateTimeOffset effectiveAtUtc,
            long effectiveTickId = 0)
        {
            return new CitySystemsResourceDemandSnapshot(
                FuelDemandPressureIndex: 0m,
                SparePartsDemandPressureIndex: 0m,
                FiltersDemandPressureIndex: 0m,
                EmergencyWaterDemandPressureIndex: 0m,
                OverallDemandPressureIndex: 0m,
                EffectiveTickId: Math.Max(
                    val1: 0,
                    val2: effectiveTickId),
                EffectiveAtUtc: effectiveAtUtc.Offset == TimeSpan.Zero
                    ? effectiveAtUtc
                    : effectiveAtUtc.ToUniversalTime());
        }
    }
}
