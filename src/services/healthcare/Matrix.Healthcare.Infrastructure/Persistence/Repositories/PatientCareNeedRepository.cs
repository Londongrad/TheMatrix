using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class PatientCareNeedRepository(HealthcareDbContext dbContext)
    : IPatientCareNeedRepository
{
    public async Task<IReadOnlyList<PatientCareNeed>> GetByPatientIdsAsync(
        IReadOnlyCollection<PatientId> patientIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patientIds);
        if (patientIds.Count == 0)
            return [];

        PatientId[] ids = patientIds.Distinct().ToArray();
        return await dbContext.PatientCareNeeds
           .Where(careNeed => ids.Contains(careNeed.Id))
           .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(
        IReadOnlyCollection<PatientCareNeed> careNeeds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(careNeeds);
        return dbContext.PatientCareNeeds.AddRangeAsync(
            entities: careNeeds,
            cancellationToken: cancellationToken);
    }
}
