using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using MediatR;

namespace Matrix.Education.Application.Students.GetStudentEducationStatus;

public sealed class GetStudentEducationStatusQueryHandler(
    IStudentEducationStatusReader statusReader)
    : IRequestHandler<GetStudentEducationStatusQuery, StudentEducationStatusView?>
{
    public Task<StudentEducationStatusView?> Handle(
        GetStudentEducationStatusQuery request,
        CancellationToken cancellationToken)
    {
        return statusReader.GetAsync(
            simulationHostId: new SimulationHostId(request.SimulationHostId),
            residentId: new ResidentId(request.ResidentId),
            cancellationToken: cancellationToken);
    }
}
