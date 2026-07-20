using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Application.Integration
{
    public interface IPopulationResidentHealthRiskOutboxWriter
    {
        Task AddResidentHealthRiskBatchAsync(
            PopulationResidentHealthRiskBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task AddResidentHealthRiskBatchAsync(
            PopulationResidentHealthRiskBatchV2 batch,
            CancellationToken cancellationToken = default);
    }
}
