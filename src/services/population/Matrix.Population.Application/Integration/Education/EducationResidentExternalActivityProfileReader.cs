using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Integration.Education
{
    public sealed class EducationResidentExternalActivityProfileReader(
        IEducationParticipationProjectionRepository participationProjectionRepository)
        : IResidentExternalActivityProfileReader
    {
        public async Task<IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile>> ReadAsync(
            Guid simulationHostId,
            IReadOnlyCollection<PersonEntity> residents,
            ResidentExternalActivityReadScope scope,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(residents);

            IReadOnlyDictionary<Guid, EducationParticipationProjection> projections = scope switch
            {
                ResidentExternalActivityReadScope.None =>
                    new Dictionary<Guid, EducationParticipationProjection>(),
                ResidentExternalActivityReadScope.ActiveOnly when residents.Count == 0 =>
                    new Dictionary<Guid, EducationParticipationProjection>(),
                ResidentExternalActivityReadScope.ActiveOnly =>
                    await participationProjectionRepository.GetEnrolledByResidentIdsAsync(
                        simulationHostId: simulationHostId,
                        residentIds: residents.Select(resident => resident.Id.Value).ToArray(),
                        cancellationToken: cancellationToken),
                ResidentExternalActivityReadScope.All when residents.Count == 0 =>
                    new Dictionary<Guid, EducationParticipationProjection>(),
                ResidentExternalActivityReadScope.All =>
                    await participationProjectionRepository.GetByResidentIdsAsync(
                        simulationHostId: simulationHostId,
                        residentIds: residents.Select(resident => resident.Id.Value).ToArray(),
                        cancellationToken: cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported activity read scope.")
            };
            var participationIndex = new EducationParticipationProjectionIndex(
                simulationHostId: simulationHostId,
                projections: projections);

            return residents.ToDictionary(
                keySelector: resident => resident.Id,
                elementSelector: resident => EducationResidentExternalActivityProfileFactory.Create(
                    participationIndex.FindCurrent(resident)));
        }
    }
}
