using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;

public sealed class CityHealthcarePressureSnapshotRepository(PopulationDbContext dbContext)
    : ICityHealthcarePressureSnapshotRepository
{
    public async Task<ClassicCityHealthcarePressureSnapshot?> GetByCityAsync(
        CityId cityId,
        CancellationToken cancellationToken = default)
    {
        CityHealthcarePressureSnapshotEntity? entity =
            await dbContext.CityHealthcarePressureSnapshots
               .AsNoTracking()
               .Include(snapshot => snapshot.Districts)
               .SingleOrDefaultAsync(snapshot => snapshot.CityId == cityId.Value, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task UpsertAsync(
        ClassicCityHealthcarePressureSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        CityHealthcarePressureSnapshotEntity? entity =
            await dbContext.CityHealthcarePressureSnapshots
               .Include(item => item.Districts)
               .SingleOrDefaultAsync(item => item.CityId == snapshot.CityId.Value, cancellationToken);
        if (entity is null)
        {
            await dbContext.CityHealthcarePressureSnapshots.AddAsync(CreateEntity(snapshot), cancellationToken);
            return;
        }

        entity.Apply(
            snapshot.SourceRevision,
            snapshot.CurrentDate,
            snapshot.PatientCount,
            snapshot.Pressure.ActiveIllnessCount,
            snapshot.Pressure.SevereIllnessCount,
            snapshot.Pressure.MedicalLoadIndex,
            snapshot.Pressure.TriagePressureIndex,
            snapshot.Pressure.RecoverySupportIndex,
            snapshot.OccurredAtUtc,
            snapshot.UpdatedAtUtc,
            MapDistricts(snapshot));
    }

    public async Task DeleteByCityAsync(
        CityId cityId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.CityHealthcarePressureSnapshots
           .Where(snapshot => snapshot.CityId == cityId.Value)
           .ExecuteDeleteAsync(cancellationToken);
    }

    private static CityHealthcarePressureSnapshotEntity CreateEntity(
        ClassicCityHealthcarePressureSnapshot snapshot)
    {
        return new CityHealthcarePressureSnapshotEntity(
            snapshot.CityId.Value,
            snapshot.SourceRevision,
            snapshot.CurrentDate,
            snapshot.PatientCount,
            snapshot.Pressure.ActiveIllnessCount,
            snapshot.Pressure.SevereIllnessCount,
            snapshot.Pressure.MedicalLoadIndex,
            snapshot.Pressure.TriagePressureIndex,
            snapshot.Pressure.RecoverySupportIndex,
            snapshot.OccurredAtUtc,
            snapshot.UpdatedAtUtc,
            MapDistricts(snapshot));
    }

    private static ClassicCityHealthcarePressureSnapshot Map(
        CityHealthcarePressureSnapshotEntity entity)
    {
        return new ClassicCityHealthcarePressureSnapshot(
            CityId.From(entity.CityId),
            entity.SourceRevision,
            entity.CurrentDate,
            entity.PatientCount,
            new CityPopulationHealthcarePressureProfile(
                entity.ActiveIllnessCount,
                entity.SevereIllnessCount,
                entity.MedicalLoadIndex,
                entity.TriagePressureIndex,
                entity.RecoverySupportIndex),
            entity.OccurredAtUtc,
            entity.UpdatedAtUtc,
            entity.Districts
               .OrderBy(district => district.DistrictId)
               .Select(district => new ClassicCityHealthcareDistrictHealthSnapshot(
                    DistrictId.From(district.DistrictId),
                    district.PatientCount,
                    district.ActiveIllnessCount,
                    district.SevereIllnessCount))
               .ToArray());
    }

    private static CityHealthcareDistrictHealthSnapshotEntity[] MapDistricts(
        ClassicCityHealthcarePressureSnapshot snapshot)
    {
        return snapshot.Districts
           .Select(district => new CityHealthcareDistrictHealthSnapshotEntity(
                snapshot.CityId.Value,
                district.DistrictId.Value,
                district.PatientCount,
                district.ActiveIllnessCount,
                district.SevereIllnessCount))
           .ToArray();
    }
}
