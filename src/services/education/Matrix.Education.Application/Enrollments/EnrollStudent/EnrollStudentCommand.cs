using MediatR;

namespace Matrix.Education.Application.Enrollments.EnrollStudent
{
    public sealed record EnrollStudentCommand(
        Guid SimulationHostId,
        Guid ResidentId,
        Guid InstitutionId,
        string Stage,
        DateOnly EnrolledOn)
        : IRequest<EnrollStudentResult>;
}
