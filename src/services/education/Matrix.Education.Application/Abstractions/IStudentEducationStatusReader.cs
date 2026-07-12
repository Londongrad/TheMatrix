using Matrix.Education.Application.Students.GetStudentEducationStatus;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Abstractions;

public interface IStudentEducationStatusReader
{
    Task<StudentEducationStatusView?> GetAsync(
        SimulationHostId simulationHostId,
        ResidentId residentId,
        CancellationToken cancellationToken = default);
}
