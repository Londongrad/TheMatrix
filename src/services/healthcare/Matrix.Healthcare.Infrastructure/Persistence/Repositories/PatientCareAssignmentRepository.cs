using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class PatientCareAssignmentRepository(HealthcareDbContext dbContext)
    : IPatientCareAssignmentRepository
{
    public async Task<IReadOnlyList<PatientCareAssignment>> GetDueScheduledByPatientIdsAsync(
        SimulationHostId simulationHostId,
        IReadOnlyCollection<PatientId> patientIds,
        DateOnly dueThroughDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patientIds);
        if (patientIds.Count == 0)
            return [];

        PatientId[] ids = patientIds.Distinct().ToArray();
        return await BuildDueScheduledQuery(
                simulationHostId,
                ids,
                dueThroughDate)
           .ToListAsync(cancellationToken);
    }

    internal IQueryable<PatientCareAssignment> BuildDueScheduledQuery(
        SimulationHostId simulationHostId,
        IReadOnlyCollection<PatientId> patientIds,
        DateOnly dueThroughDate)
    {
        PatientId[] ids = patientIds.Distinct().ToArray();

        return dbContext.PatientCareAssignments
           .Where(assignment =>
                assignment.SimulationHostId == simulationHostId
                && ids.Contains(assignment.PatientId)
                && assignment.Status == PatientCareAssignmentStatus.Scheduled
                && assignment.CareDate <= dueThroughDate
                && !dbContext.PatientCareAssignments.Any(earlier =>
                    earlier.SimulationHostId == simulationHostId
                    && earlier.PatientId == assignment.PatientId
                    && earlier.Status == PatientCareAssignmentStatus.Scheduled
                    && earlier.CareDate <= dueThroughDate
                    && earlier.CareDate < assignment.CareDate))
           .OrderBy(assignment => assignment.PatientId)
           .ThenBy(assignment => assignment.CareDate);
    }
}
