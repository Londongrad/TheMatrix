namespace Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;

public sealed record ClassicCityLearningConditions(
    int AgeYears, int Energy, int Stress, int FunctionalCapacity, bool IsHomeless,
    decimal RoadAccessibility, decimal PowerCoverage, decimal WaterCoverage, decimal HeatingCoverage,
    decimal Flooding, decimal FoodShortage, decimal EmergencyWaterShortage, bool EmergencyRationing,
    bool HasCommuteData, bool IsCommuteAccessible, decimal CommuteAccessibility);
