using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
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

        public async Task<PatientPopulationHealthBurden> GetPopulationHealthBurdenAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            var aggregate = await dbContext.PatientMedicalRecords
               .Where(record => record.SimulationHostId == simulationHostId)
               .GroupBy(_ => 1)
               .Select(records => new
                {
                    PatientCount = records.Count(),
                    MildIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Mild),
                    ModerateIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Moderate),
                    SevereIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Severe)
                })
               .SingleOrDefaultAsync(cancellationToken);

            return aggregate is null
                ? PatientPopulationHealthBurden.Empty
                : new PatientPopulationHealthBurden(
                    aggregate.PatientCount,
                    aggregate.MildIllnessCount,
                    aggregate.ModerateIllnessCount,
                    aggregate.SevereIllnessCount);
        }

        public async Task<IReadOnlyList<PatientCommunityHealthBurden>> GetCommunityHealthBurdensAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            var aggregates = await dbContext.PatientMedicalRecords
               .Where(record => record.SimulationHostId == simulationHostId
                                && record.CommunityId.HasValue)
               .GroupBy(record => record.CommunityId!.Value)
               .Select(records => new
                {
                    CommunityId = records.Key,
                    PatientCount = records.Count(),
                    MildIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Mild),
                    ModerateIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Moderate),
                    SevereIllnessCount = records.Count(record =>
                        record.Illness.CurrentSeverity == IllnessSeverity.Severe)
                })
               .OrderBy(aggregate => aggregate.CommunityId)
               .ToArrayAsync(cancellationToken);

            return aggregates
               .Select(aggregate => new PatientCommunityHealthBurden(
                    aggregate.CommunityId,
                    new PatientPopulationHealthBurden(
                        aggregate.PatientCount,
                        aggregate.MildIllnessCount,
                        aggregate.ModerateIllnessCount,
                        aggregate.SevereIllnessCount)))
               .ToArray();
        }

        public async Task<IReadOnlyDictionary<PatientHouseholdId, int>>
            GetInfectiousPatientCountsByHouseholdAsync(
                SimulationHostId simulationHostId,
                IReadOnlyCollection<PatientHouseholdId> householdIds,
                CancellationToken cancellationToken = default)
        {
            if (householdIds.Count == 0)
                return new Dictionary<PatientHouseholdId, int>();

            var aggregates = await (
                    from profile in dbContext.PatientProfiles
                    join record in dbContext.PatientMedicalRecords on profile.Id equals record.Id
                    where profile.SimulationHostId == simulationHostId
                          && record.SimulationHostId == simulationHostId
                          && profile.IsAlive
                          && profile.IsActive
                          && profile.HouseholdId.HasValue
                          && householdIds.Contains(profile.HouseholdId.Value)
                          && record.Illness.CurrentKind == IllnessKind.Infection
                    group profile by profile.HouseholdId!.Value
                    into household
                    select new
                    {
                        HouseholdId = household.Key,
                        InfectiousPatientCount = household.Count()
                    })
               .ToArrayAsync(cancellationToken);

            return aggregates.ToDictionary(
                aggregate => aggregate.HouseholdId,
                aggregate => aggregate.InfectiousPatientCount);
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
