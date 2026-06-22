using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Application.Integration
{
    public interface IPopulationResidentFactsOutboxWriter
    {
        Task AddResidentFactsBatchAsync(
            PopulationResidentFactsBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
