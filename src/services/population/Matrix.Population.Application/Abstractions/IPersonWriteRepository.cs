using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface IPersonWriteRepository
    {
        Task DeleteAllAsync(CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Person person,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<Person> persons,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Person person,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Person person,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
