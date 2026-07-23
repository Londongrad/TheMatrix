using Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;
using MediatR;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Attendance;

public sealed record LearningAttendanceInput(Guid ResidentId, long LifecycleRevision, long ParticipationRevision,
    LearningAttendanceConditions Conditions);

public sealed record LearningAttendanceConditions(
    int AgeYears, int Energy, int Stress, int FunctionalCapacity, bool IsHomeless,
    decimal RoadAccessibility, decimal PowerCoverage, decimal WaterCoverage, decimal HeatingCoverage,
    decimal Flooding, decimal FoodShortage, decimal EmergencyWaterShortage, bool EmergencyRationing,
    bool HasCommuteData, bool IsCommuteAccessible, decimal CommuteAccessibility)
{
    internal ClassicCityLearningConditions ToDomain() => new(AgeYears, Energy, Stress, FunctionalCapacity, IsHomeless,
        RoadAccessibility, PowerCoverage, WaterCoverage, HeatingCoverage, Flooding, FoodShortage,
        EmergencyWaterShortage, EmergencyRationing, HasCommuteData, IsCommuteAccessible, CommuteAccessibility);
}

public sealed record EvaluateLearningAttendanceCommand(Guid SimulationHostId, long SourceTickId,
    DateTimeOffset ObservedAtSimTimeUtc, IReadOnlyList<LearningAttendanceInput> Residents) : IRequest<int>;
