using MediatR;

namespace Matrix.Education.Application.Students.GetStudentEducationStatus;

public sealed record GetStudentEducationStatusQuery(
    Guid SimulationHostId,
    Guid ResidentId)
    : IRequest<StudentEducationStatusView?>;
