using MediatR;

namespace Matrix.Education.Application.Enrollments.CompleteStudentStage
{
    public sealed record CompleteStudentStageCommand(
        Guid SimulationHostId,
        Guid ResidentId,
        DateOnly CompletedOn)
        : IRequest<CompleteStudentStageResult>;
}
