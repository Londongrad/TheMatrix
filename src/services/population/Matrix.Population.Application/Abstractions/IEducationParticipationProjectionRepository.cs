using Matrix.Population.Application.Integration.Education;

namespace Matrix.Population.Application.Abstractions
{
    public interface IEducationParticipationProjectionRepository
    {
        Task<int> UpsertNewerAsync(
            IReadOnlyCollection<EducationParticipationProjection> projections,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, EducationParticipationProjection>> GetByResidentIdsAsync(
            Guid simulationHostId,
            IReadOnlyCollection<Guid> residentIds,
            CancellationToken cancellationToken = default);

        Task DeleteBySimulationHostAsync(
            Guid simulationHostId,
            CancellationToken cancellationToken = default);
    }
}
