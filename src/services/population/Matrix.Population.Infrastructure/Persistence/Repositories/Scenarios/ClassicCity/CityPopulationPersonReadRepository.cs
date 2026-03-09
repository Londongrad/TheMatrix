using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationPersonReadRepository(PopulationDbContext dbContext)
        : ICityPopulationPersonReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageByCityAsync(
            CityId cityId,
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Person> query = _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person);

            int totalCount = await query.CountAsync(cancellationToken);

            List<Person> items = await query
               .OrderBy(person => person.Id)
               .Skip(pagination.Skip)
               .Take(pagination.PageSize)
               .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Person?> FindByCityAndPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .FirstOrDefaultAsync(
                    predicate: person => person.Id == personId,
                    cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyCollection<Person>> ListChildrenByParentIdAsync(
            CityId cityId,
            PersonId parentId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .Where(person => person.MotherId == parentId || person.FatherId == parentId)
               .ToListAsync(cancellationToken);
        }

        public async Task<CityId?> FindCityIdByPersonIdAsync(
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            Guid? cityId = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where person.Id == personId
                select (Guid?)placement.CityId.Value)
               .FirstOrDefaultAsync(cancellationToken);

            return cityId.HasValue
                ? CityId.From(cityId.Value)
                : null;
        }

        public async Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId && person.Id == personId
                select new
                {
                    HouseholdId = person.HouseholdId.Value,
                    placement.HousingStatus,
                    ResidentialBuildingId = placement.ResidentialBuildingId.HasValue
                        ? placement.ResidentialBuildingId.Value.Value
                        : (Guid?)null
                })
               .FirstOrDefaultAsync(cancellationToken);

            return snapshot is null
                ? null
                : new CityResidentHousingSnapshot(
                    HouseholdId: HouseholdId.From(snapshot.HouseholdId),
                    HousingStatus: snapshot.HousingStatus,
                    ResidentialBuildingId: snapshot.ResidentialBuildingId.HasValue
                        ? ResidentialBuildingId.From(snapshot.ResidentialBuildingId.Value)
                        : null);
        }

        public async Task<IReadOnlyDictionary<HouseholdId, HousingStatus>> ListHousingStatusesByHouseholdAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            var rows = await _dbContext.ClassicCityHouseholdPlacements
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .Select(x => new
                {
                    HouseholdId = x.HouseholdId.Value,
                    x.HousingStatus
                })
               .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                keySelector: x => HouseholdId.From(x.HouseholdId),
                elementSelector: x => x.HousingStatus);
        }

        public async Task<IReadOnlyCollection<CityEmploymentWorkplaceSnapshot>> ListEmploymentWorkplacesAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            var snapshots = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId
                    && person.Employment.Status == Domain.Enums.EmploymentStatus.Employed
                    && person.Employment.Job != null
                group person by new
                {
                    WorkplaceId = person.Employment.Job!.WorkplaceId.Value,
                    JobTitle = person.Employment.Job.Title
                }
                into workplaceGroup
                orderby workplaceGroup.Count() descending, workplaceGroup.Key.JobTitle
                select new
                {
                    workplaceGroup.Key.WorkplaceId,
                    workplaceGroup.Key.JobTitle,
                    ResidentCount = workplaceGroup.Count()
                })
               .ToListAsync(cancellationToken);

            return snapshots
               .Select(x => new CityEmploymentWorkplaceSnapshot(
                    WorkplaceId: WorkplaceId.From(x.WorkplaceId),
                    JobTitle: x.JobTitle,
                    ResidentCount: x.ResidentCount))
               .ToArray();
        }

        public async Task<CityEmploymentWorkplaceSnapshot?> FindEmploymentWorkplaceByIdAsync(
            CityId cityId,
            WorkplaceId workplaceId,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId
                    && person.Employment.Status == Domain.Enums.EmploymentStatus.Employed
                    && person.Employment.Job != null
                    && person.Employment.Job.WorkplaceId.Value == workplaceId.Value
                group person by new
                {
                    WorkplaceId = person.Employment.Job!.WorkplaceId.Value,
                    JobTitle = person.Employment.Job.Title
                }
                into workplaceGroup
                select new
                {
                    workplaceGroup.Key.WorkplaceId,
                    workplaceGroup.Key.JobTitle,
                    ResidentCount = workplaceGroup.Count()
                })
               .FirstOrDefaultAsync(cancellationToken);

            return snapshot is null
                ? null
                : new CityEmploymentWorkplaceSnapshot(
                    WorkplaceId: WorkplaceId.From(snapshot.WorkplaceId),
                    JobTitle: snapshot.JobTitle,
                    ResidentCount: snapshot.ResidentCount);
        }

        public async Task<IReadOnlyCollection<CityEducationInstitutionSnapshot>> ListEducationInstitutionsAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            var snapshots = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId
                    && person.Employment.Status == Domain.Enums.EmploymentStatus.Student
                    && person.Education.CurrentInstitutionId != null
                group person by new
                {
                    InstitutionId = person.Education.CurrentInstitutionId!.Value,
                    person.Education.Level
                }
                into institutionGroup
                orderby institutionGroup.Count() descending, institutionGroup.Key.Level
                select new
                {
                    institutionGroup.Key.InstitutionId,
                    institutionGroup.Key.Level,
                    ResidentCount = institutionGroup.Count()
                })
               .ToListAsync(cancellationToken);

            return snapshots
               .Select(x => new CityEducationInstitutionSnapshot(
                    InstitutionId: EducationInstitutionId.From(x.InstitutionId),
                    EducationLevel: x.Level,
                    ResidentCount: x.ResidentCount))
               .ToArray();
        }

        public async Task<CityEducationInstitutionSnapshot?> FindEducationInstitutionByIdAsync(
            CityId cityId,
            EducationInstitutionId institutionId,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId
                    && person.Employment.Status == Domain.Enums.EmploymentStatus.Student
                    && person.Education.CurrentInstitutionId != null
                    && person.Education.CurrentInstitutionId.Value == institutionId.Value
                group person by new
                {
                    InstitutionId = person.Education.CurrentInstitutionId!.Value,
                    person.Education.Level
                }
                into institutionGroup
                select new
                {
                    institutionGroup.Key.InstitutionId,
                    institutionGroup.Key.Level,
                    ResidentCount = institutionGroup.Count()
                })
               .FirstOrDefaultAsync(cancellationToken);

            return snapshot is null
                ? null
                : new CityEducationInstitutionSnapshot(
                    InstitutionId: EducationInstitutionId.From(snapshot.InstitutionId),
                    EducationLevel: snapshot.Level,
                    ResidentCount: snapshot.ResidentCount);
        }
    }
}
