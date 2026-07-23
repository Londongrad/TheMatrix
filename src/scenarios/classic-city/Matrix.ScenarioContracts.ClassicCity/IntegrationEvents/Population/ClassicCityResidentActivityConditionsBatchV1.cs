namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

public sealed record ClassicCityActivityAreaConditionsV1(
    Guid? DistrictId, decimal RoadAccessibility, decimal PowerCoverage, decimal WaterCoverage,
    decimal HeatingCoverage, decimal Flooding, decimal FoodShortage, decimal EmergencyWaterShortage, bool EmergencyRationing);

public sealed record ClassicCityResidentActivityConditionsV1(
    Guid ResidentId, long ResidentLifecycleRevision, long ActivityRevision, int AreaIndex,
    int AgeYears, int Energy, int Stress, int FunctionalCapacity, bool IsHomeless,
    bool HasCommuteData, bool IsCommuteAccessible, decimal CommuteAccessibility);

public sealed record ClassicCityResidentActivityConditionsBatchV1(
    Guid SimulationHostId, long SourceTickId, DateTimeOffset ObservedAtSimTimeUtc, DateTimeOffset OccurredAtUtc,
    int BatchNumber, int TotalBatches, IReadOnlyList<ClassicCityActivityAreaConditionsV1> Areas,
    IReadOnlyList<ClassicCityResidentActivityConditionsV1> Residents);
