using MassTransit;
using Matrix.Education.Application.Scenarios.ClassicCity.Attendance;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using MediatR;

namespace Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityLearningAttendanceConsumer(IMediator mediator)
    : IConsumer<ClassicCityResidentActivityConditionsBatchV1>
{
    public Task Consume(ConsumeContext<ClassicCityResidentActivityConditionsBatchV1> context) =>
        mediator.Send(Map(context.Message), context.CancellationToken);

    internal static EvaluateLearningAttendanceCommand Map(ClassicCityResidentActivityConditionsBatchV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Residents is null || message.Residents.Count is < 1 or > 1000
            || message.Areas is null || message.Areas.Count < 1 || message.Areas.Count > message.Residents.Count
            || message.Areas.Any(area => area is null || area.DistrictId == Guid.Empty)
            || message.BatchNumber < 1 || message.TotalBatches < message.BatchNumber
            || message.OccurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Invalid activity conditions envelope.", nameof(message));

        var inputs = new LearningAttendanceInput[message.Residents.Count];
        for (int index = 0; index < inputs.Length; index++)
        {
            var resident = message.Residents[index];
            if (resident is null || resident.AreaIndex < 0 || resident.AreaIndex >= message.Areas.Count)
                throw new ArgumentException("Invalid activity area reference.", nameof(message));
            var area = message.Areas[resident.AreaIndex];
            inputs[index] = new(resident.ResidentId, resident.ResidentLifecycleRevision, resident.ActivityRevision,
                new(resident.AgeYears, resident.Energy, resident.Stress, resident.FunctionalCapacity, resident.IsHomeless,
                    area.RoadAccessibility, area.PowerCoverage, area.WaterCoverage, area.HeatingCoverage, area.Flooding,
                    area.FoodShortage, area.EmergencyWaterShortage, area.EmergencyRationing,
                    resident.HasCommuteData, resident.IsCommuteAccessible, resident.CommuteAccessibility));
        }
        return new(message.SimulationHostId, message.SourceTickId, message.ObservedAtSimTimeUtc, inputs);
    }
}
