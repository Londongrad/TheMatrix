using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityPopulationSignalPublisher
    {
        Task PublishClassicCityEmployerFinancialStressBatchAsync(
            ClassicCityEmployerFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task PublishClassicCityHouseholdFinancialStressBatchAsync(
            ClassicCityHouseholdFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
