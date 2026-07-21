using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Integration
{
    public interface IResidentExternalActivityProfileReader
    {
        Task<IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile>> ReadAsync(
            Guid simulationHostId,
            IReadOnlyCollection<PersonEntity> residents,
            ResidentExternalActivityReadScope scope,
            CancellationToken cancellationToken);
    }
}
