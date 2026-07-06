using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Application.Integration
{
    public interface IPopulationResidentVitalStateOutboxWriter
    {
        Task AddResidentVitalStateBatchAsync(
            PopulationResidentVitalStateBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
