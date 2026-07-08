using Matrix.Healthcare.Application.Operations;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPopulationHealthSnapshotOutboxWriter
{
    Task AddAsync(
        PopulationHealthSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
