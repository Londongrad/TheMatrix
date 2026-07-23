namespace Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;

public sealed class ClassicCityLearningAttendancePolicy
{
    public decimal Evaluate(ClassicCityLearningConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.AgeYears is < 0 or > 120 || conditions.Energy is < 0 or > 100
            || conditions.Stress is < 0 or > 100 || conditions.FunctionalCapacity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(conditions), "Resident facts are outside their supported ranges.");
        foreach (decimal value in new[] { conditions.RoadAccessibility, conditions.PowerCoverage, conditions.WaterCoverage,
                     conditions.HeatingCoverage, conditions.Flooding, conditions.FoodShortage, conditions.EmergencyWaterShortage,
                     conditions.CommuteAccessibility })
            if (value is < 0m or > 2m)
                throw new ArgumentOutOfRangeException(nameof(conditions), "Condition indices must be between zero and two.");

        double attendance = 1d
            - Deficit(conditions.RoadAccessibility) * 0.30d
            - Pressure(conditions.Flooding) * 0.20d
            - Deficit(conditions.PowerCoverage) * 0.16d
            - Deficit(conditions.WaterCoverage) * 0.09d
            - Deficit(conditions.HeatingCoverage) * 0.08d
            - Pressure(conditions.FoodShortage) * 0.10d
            - Pressure(conditions.EmergencyWaterShortage) * 0.06d
            - (conditions.EmergencyRationing ? 0.15d : 0d) * 0.05d
            - Math.Clamp((35d - conditions.Energy) / 35d, 0d, 1d) * 0.24d
            - Math.Clamp((conditions.Stress - 50d) / 50d, 0d, 1d) * 0.18d
            - (100d - conditions.FunctionalCapacity) / 100d * 0.45d
            - (conditions.HasCommuteData ? Math.Clamp((double)(1m - conditions.CommuteAccessibility), 0d, 1d) : 0d) * 0.34d
            - (conditions.HasCommuteData && !conditions.IsCommuteAccessible ? 0.22d : 0d)
            - (conditions.IsHomeless ? 0.12d : 0d)
            - (conditions.AgeYears < 7 ? -0.03d : 0d);
        return decimal.Round((decimal)Math.Clamp(attendance, 0.18d, 1d), 4, MidpointRounding.AwayFromZero);
    }

    private static double Deficit(decimal value) => Math.Clamp((double)(1m - value), 0d, 1.5d);
    private static double Pressure(decimal value) => Math.Clamp((double)value, 0d, 1.5d);
}
