using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface IPersonReadRepository
    {
        Task<IReadOnlyCollection<Person>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Person> GetByIdAsync(PersonId id, CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken = default);
    }
}
