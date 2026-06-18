using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Abstractions
{
    public interface IPersonLifecycleExtension
    {
        Task OnPersonDiedAsync(
            Person person,
            DateOnly fallbackCurrentDate,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default);

        Task OnPersonResurrectedAsync(
            Person person,
            DateOnly fallbackCurrentDate,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default);
    }
}
