using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Operations;

public sealed class CareOperationalProfileProvider(
    ICareServiceQualityStateRepository qualityRepository,
    ICareMedicineSupplyStateRepository medicineRepository)
    : ICareOperationalProfileProvider
{
    public async Task<CareOperationalProfile> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default)
    {
        CareServiceQualityState? quality = await qualityRepository.GetAsync(
            simulationHostId,
            cancellationToken);
        CareMedicineSupplyState? medicine = await medicineRepository.GetAsync(
            simulationHostId,
            cancellationToken);

        return new CareOperationalProfile(
            ServiceQuality: quality?.QualityMultiplier
                            ?? CareQualityMultiplier.Baseline,
            MedicineAvailability: medicine?.StockLevel
                                  ?? CareAvailabilityIndex.Full,
            MedicineShortageRisk: medicine?.ShortageRisk
                                  ?? CareAvailabilityIndex.None);
    }
}
