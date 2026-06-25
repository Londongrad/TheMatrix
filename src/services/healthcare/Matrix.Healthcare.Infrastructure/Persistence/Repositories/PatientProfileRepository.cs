using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories
{
    public sealed class PatientProfileRepository(HealthcareDbContext dbContext)
        : IPatientProfileRepository
    {
        public async Task<IReadOnlyList<PatientProfile>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default)
        {
            if (patientIds.Count == 0)
                return Array.Empty<PatientProfile>();

            return await dbContext.PatientProfiles
               .Where(profile => patientIds.Contains(profile.Id))
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<PatientProfile> profiles,
            CancellationToken cancellationToken = default)
        {
            dbContext.PatientProfiles.AddRange(profiles);
            return Task.CompletedTask;
        }
    }
}
