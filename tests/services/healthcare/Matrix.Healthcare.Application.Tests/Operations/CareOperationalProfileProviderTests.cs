using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Operations;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Operations;

public sealed class CareOperationalProfileProviderTests
{
    private static readonly SimulationHostId HostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset ObservedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task GetAsync_AvailableSignals_ComposesOperationalProfile()
    {
        CareServiceQualityState quality = CareServiceQualityState.Register(
            HostId,
            new CareQualityMultiplier(0.82m),
            ObservedAtUtc);
        CareMedicineSupplyState medicine = CareMedicineSupplyState.Register(
            HostId,
            new CareAvailabilityIndex(0.63m),
            new CareAvailabilityIndex(0.31m),
            sourceRevision: 17,
            ObservedAtUtc);
        var provider = new CareOperationalProfileProvider(
            new QualityRepositoryStub(quality),
            new MedicineRepositoryStub(medicine));

        CareOperationalProfile profile = await provider.GetAsync(HostId);

        Assert.Equal(0.82m, profile.ServiceQuality.Value);
        Assert.Equal(0.63m, profile.MedicineAvailability.Value);
        Assert.Equal(0.31m, profile.MedicineShortageRisk.Value);
    }

    [Fact]
    public async Task GetAsync_MissingSignals_ReturnsNeutralBaseline()
    {
        var provider = new CareOperationalProfileProvider(
            new QualityRepositoryStub(),
            new MedicineRepositoryStub());

        CareOperationalProfile profile = await provider.GetAsync(HostId);

        Assert.Equal(CareOperationalProfile.Baseline, profile);
    }

    private sealed class QualityRepositoryStub(CareServiceQualityState? state = null)
        : ICareServiceQualityStateRepository
    {
        public Task<CareServiceQualityState?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(state);
        }

        public Task AddAsync(
            CareServiceQualityState stateToAdd,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MedicineRepositoryStub(CareMedicineSupplyState? state = null)
        : ICareMedicineSupplyStateRepository
    {
        public Task<CareMedicineSupplyState?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(state);
        }

        public Task AddAsync(
            CareMedicineSupplyState stateToAdd,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
