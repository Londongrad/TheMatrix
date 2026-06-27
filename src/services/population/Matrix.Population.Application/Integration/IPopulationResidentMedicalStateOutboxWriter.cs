using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Application.Integration
{
    public interface IPopulationResidentMedicalStateOutboxWriter
    {
        Task AddResidentMedicalStateBatchAsync(
            PopulationResidentMedicalStateBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
