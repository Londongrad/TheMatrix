using MediatR;

namespace Matrix.Education.Application.Enrollments.WithdrawStudent
{
    public sealed record WithdrawStudentCommand(
        Guid SimulationHostId,
        Guid ResidentId,
        DateOnly WithdrawnOn)
        : IRequest<WithdrawStudentResult>;
}
