using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Abstractions
{
    public interface IPersonReadRepository
    {
        Task<IReadOnlyCollection<Person>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
