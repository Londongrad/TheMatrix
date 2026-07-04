using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Care.AllocatePatientCare;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class PatientCareAllocationRepository(HealthcareDbContext dbContext)
    : IPatientCareAllocationRepository
{
    public async Task<IReadOnlyList<CareFacility>> GetActiveFacilitiesAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CareFacilities
           .AsNoTracking()
           .Where(facility =>
                facility.SimulationHostId == simulationHostId
                && facility.IsActive)
           .OrderBy(facility => facility.Id)
           .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CareFacilityAssignmentCount>> GetAssignmentCountsAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        IReadOnlyCollection<CareFacilityId> careFacilityIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(careFacilityIds);
        if (careFacilityIds.Count == 0)
            return [];

        CareFacilityId[] ids = careFacilityIds.Distinct().ToArray();
        return await dbContext.PatientCareAssignments
           .AsNoTracking()
           .Where(assignment =>
                assignment.SimulationHostId == simulationHostId
                && assignment.CareDate == careDate
                && ids.Contains(assignment.CareFacilityId))
           .GroupBy(assignment => assignment.CareFacilityId)
           .Select(group => new CareFacilityAssignmentCount(
                group.Key,
                group.Count()))
           .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientCareNeed>> GetUnassignedCareNeedsAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumCount));

        return await dbContext.PatientCareNeeds
           .AsNoTracking()
           .Where(careNeed =>
                careNeed.SimulationHostId == simulationHostId
                && careNeed.IsActive
                && !dbContext.PatientCareAssignments.Any(assignment =>
                    assignment.SimulationHostId == simulationHostId
                    && assignment.PatientId == careNeed.Id
                    && assignment.CareDate == careDate))
           .OrderByDescending(careNeed => careNeed.Urgency)
           .ThenBy(careNeed => careNeed.RequestedOn)
           .ThenBy(careNeed => careNeed.Id)
           .Take(maximumCount)
           .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(
        IReadOnlyCollection<PatientCareAssignment> assignments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        return dbContext.PatientCareAssignments.AddRangeAsync(
            assignments,
            cancellationToken);
    }
}
