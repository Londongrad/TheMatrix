using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRoleWriteRepository
    {
        Task AddAsync(
            Role role,
            CancellationToken cancellationToken);
    }
}
