using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories
{
    public sealed class PatientMedicalRecordRepository(HealthcareDbContext dbContext)
        : IPatientMedicalRecordRepository
    {
        public async Task<IReadOnlyList<PatientMedicalRecord>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default)
        {
            if (patientIds.Count == 0)
                return Array.Empty<PatientMedicalRecord>();

            return await dbContext.PatientMedicalRecords
               .Where(record => patientIds.Contains(record.Id))
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<PatientMedicalRecord> records,
            CancellationToken cancellationToken = default)
        {
            dbContext.PatientMedicalRecords.AddRange(records);
            return Task.CompletedTask;
        }
    }
}
