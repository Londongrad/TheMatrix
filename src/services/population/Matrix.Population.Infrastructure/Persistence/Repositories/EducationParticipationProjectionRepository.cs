using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.ValueObjects;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class EducationParticipationProjectionRepository(PopulationDbContext dbContext)
        : IEducationParticipationProjectionRepository
    {
        public async Task<int> UpsertNewerAsync(
            IReadOnlyCollection<EducationParticipationProjection> projections,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(projections);
            if (projections.Count == 0)
                return 0;

            Guid[] hostIds = projections
               .Select(projection => projection.SimulationHostId)
               .Distinct()
               .ToArray();
            if (hostIds.Length != 1)
                throw new ArgumentException(
                    message: "Education participation upserts must target one simulation host.",
                    paramName: nameof(projections));

            Guid hostId = hostIds[0];
            PersonId[] residentIds = projections
               .Select(projection => PersonId.From(projection.ResidentId))
               .Distinct()
               .ToArray();
            if (residentIds.Length != projections.Count)
                throw new ArgumentException(
                    message: "Education participation residents must be unique.",
                    paramName: nameof(projections));

            List<EducationParticipationProjectionEntity> existing = await dbContext
               .EducationParticipationProjections
               .Where(projection => projection.SimulationHostId == hostId
                                    && residentIds.Contains(projection.ResidentId))
               .ToListAsync(cancellationToken);
            Dictionary<Guid, EducationParticipationProjectionEntity> existingByResidentId =
                existing.ToDictionary(projection => projection.ResidentId.Value);
            var added = new List<EducationParticipationProjectionEntity>();
            int applied = 0;

            foreach (EducationParticipationProjection projection in projections)
            {
                if (existingByResidentId.TryGetValue(
                        projection.ResidentId,
                        out EducationParticipationProjectionEntity? entity))
                {
                    if (entity.TryApply(projection))
                        applied++;
                    continue;
                }

                EducationParticipationProjectionEntity created =
                    EducationParticipationProjectionEntity.Create(projection);
                added.Add(created);
                existingByResidentId.Add(projection.ResidentId, created);
                applied++;
            }

            if (added.Count > 0)
                await dbContext.EducationParticipationProjections.AddRangeAsync(
                    added,
                    cancellationToken);

            return applied;
        }

        public async Task<IReadOnlyDictionary<Guid, EducationParticipationProjection>>
            GetByResidentIdsAsync(
                Guid simulationHostId,
                IReadOnlyCollection<Guid> residentIds,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(residentIds);
            if (residentIds.Count == 0)
                return new Dictionary<Guid, EducationParticipationProjection>();

            Guid[] distinctIds = residentIds.Distinct().ToArray();
            PersonId[] personIds = distinctIds.Select(PersonId.From).ToArray();
            List<EducationParticipationProjectionEntity> projections = await dbContext
               .EducationParticipationProjections
               .AsNoTracking()
               .Where(projection => projection.SimulationHostId == simulationHostId
                                    && personIds.Contains(projection.ResidentId))
               .ToListAsync(cancellationToken);
            return projections.ToDictionary(
                projection => projection.ResidentId.Value,
                projection => projection.ToProjection());
        }

        public async Task DeleteBySimulationHostAsync(
            Guid simulationHostId,
            CancellationToken cancellationToken = default)
        {
            List<EducationParticipationProjectionEntity> projections = await dbContext
               .EducationParticipationProjections
               .Where(projection => projection.SimulationHostId == simulationHostId)
               .ToListAsync(cancellationToken);
            if (projections.Count > 0)
                dbContext.EducationParticipationProjections.RemoveRange(projections);
        }
    }
}
