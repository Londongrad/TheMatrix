using MassTransit;
using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Integration.Education.ApplyEducationAttendance;
using MediatR;

namespace Matrix.Population.Infrastructure.Consumers.Education;

public sealed class EducationAttendanceConsumer(IMediator mediator) : IConsumer<EducationAttendanceEvaluatedBatchV1>
{
    public Task Consume(ConsumeContext<EducationAttendanceEvaluatedBatchV1> context) =>
        mediator.Send(Map(context.Message), context.CancellationToken);

    internal static ApplyEducationAttendanceCommand Map(EducationAttendanceEvaluatedBatchV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.Residents is null || message.Residents.Count is < 1 or > 1000
            || message.Residents.Any(resident => resident is null) || message.OccurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Invalid education attendance envelope.", nameof(message));
        return new(message.SimulationHostId, message.SourceTickId, message.ObservedAtSimTimeUtc,
            message.Residents.Select(resident => new EducationAttendanceInput(resident.ResidentId,
                resident.ResidentLifecycleRevision, resident.ParticipationRevision, resident.AttendanceIndex,
                resident.CommuteAccessibilityIndex)).ToArray());
    }
}
