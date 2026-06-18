using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationSignalPublisher
    {
        Task PublishClassicCityCostOfLivingSnapshotAsync(
            ClassicCityCostOfLivingSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);

        Task PublishClassicCityServiceQualitySnapshotAsync(
            ClassicCityServiceQualitySnapshotV1 snapshot,
            CancellationToken cancellationToken = default);

        Task PublishClassicCityEmployerFinancialStressBatchAsync(
            ClassicCityEmployerFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task PublishClassicCityHouseholdFinancialStressBatchAsync(
            ClassicCityHouseholdFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
