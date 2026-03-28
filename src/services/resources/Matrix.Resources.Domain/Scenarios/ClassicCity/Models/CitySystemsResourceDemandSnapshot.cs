namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CitySystemsResourceDemandSnapshot(
        decimal FuelDemandPressureIndex,
        decimal SparePartsDemandPressureIndex,
        decimal FiltersDemandPressureIndex,
        decimal EmergencyWaterDemandPressureIndex,
        decimal OverallDemandPressureIndex,
        DateTimeOffset EffectiveAtUtc)
    {
        public static CitySystemsResourceDemandSnapshot Neutral(DateTimeOffset effectiveAtUtc)
        {
            return new CitySystemsResourceDemandSnapshot(
                FuelDemandPressureIndex: 0m,
                SparePartsDemandPressureIndex: 0m,
                FiltersDemandPressureIndex: 0m,
                EmergencyWaterDemandPressureIndex: 0m,
                OverallDemandPressureIndex: 0m,
                EffectiveAtUtc: effectiveAtUtc.Offset == TimeSpan.Zero
                    ? effectiveAtUtc
                    : effectiveAtUtc.ToUniversalTime());
        }
    }
}
