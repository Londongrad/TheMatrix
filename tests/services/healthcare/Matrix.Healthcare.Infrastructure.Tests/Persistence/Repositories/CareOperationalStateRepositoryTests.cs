using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories;

public sealed class CareOperationalStateRepositoryTests
{
    private static readonly SimulationHostId HostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset ObservedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task ServiceQualityRepository_AddAndGet_PreservesState()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new CareServiceQualityStateRepository(dbContext);
        CareServiceQualityState state = CareServiceQualityState.Register(
            HostId,
            new CareQualityMultiplier(0.82m),
            ObservedAtUtc);

        await repository.AddAsync(state);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        CareServiceQualityState? loaded = await repository.GetAsync(HostId);

        Assert.NotNull(loaded);
        Assert.Equal(0.82m, loaded.QualityMultiplier.Value);
        Assert.Equal(ObservedAtUtc, loaded.LastObservedAtUtc);
    }

    [Fact]
    public async Task MedicineSupplyRepository_AddAndGet_PreservesState()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new CareMedicineSupplyStateRepository(dbContext);
        CareMedicineSupplyState state = CareMedicineSupplyState.Register(
            HostId,
            new CareAvailabilityIndex(0.63m),
            new CareAvailabilityIndex(0.31m),
            sourceRevision: 17,
            ObservedAtUtc);

        await repository.AddAsync(state);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        CareMedicineSupplyState? loaded = await repository.GetAsync(HostId);

        Assert.NotNull(loaded);
        Assert.Equal(0.63m, loaded.StockLevel.Value);
        Assert.Equal(0.31m, loaded.ShortageRisk.Value);
        Assert.Equal(17, loaded.LastSourceRevision);
        Assert.Equal(ObservedAtUtc, loaded.LastObservedAtUtc);
    }
}
