using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories;

public sealed class PatientCareNeedRepositoryTests
{
    private static readonly DateTimeOffset AssessedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task AddRangeAndGetByPatientIds_PersistsOnlyRequestedCareNeeds()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new PatientCareNeedRepository(dbContext);
        PatientCareNeed first = CreateCareNeed(Guid.NewGuid(), CareNeedUrgency.Emergency);
        PatientCareNeed second = CreateCareNeed(Guid.NewGuid(), CareNeedUrgency.Urgent);
        PatientCareNeed unrequested = CreateCareNeed(Guid.NewGuid(), CareNeedUrgency.Routine);

        await repository.AddRangeAsync([first, second, unrequested]);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        IReadOnlyList<PatientCareNeed> loaded = await repository.GetByPatientIdsAsync(
            [first.PatientId, second.PatientId]);

        Assert.Equal(2, loaded.Count);
        Assert.Contains(loaded, careNeed => careNeed.PatientId == first.PatientId);
        Assert.Contains(loaded, careNeed => careNeed.PatientId == second.PatientId);
        Assert.DoesNotContain(loaded, careNeed => careNeed.PatientId == unrequested.PatientId);
        Assert.All(loaded, careNeed => Assert.Equal(
            EntityState.Unchanged,
            dbContext.Entry(careNeed).State));
    }

    [Fact]
    public async Task GetByPatientIds_EmptyIds_ReturnsWithoutTrackingCareNeeds()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new PatientCareNeedRepository(dbContext);
        dbContext.PatientCareNeeds.Add(CreateCareNeed(Guid.NewGuid(), CareNeedUrgency.Routine));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        IReadOnlyList<PatientCareNeed> loaded = await repository.GetByPatientIdsAsync([]);

        Assert.Empty(loaded);
        Assert.Empty(dbContext.ChangeTracker.Entries<PatientCareNeed>());
    }

    [Fact]
    public void BatchLookup_TranslatesStrongIdsToPostgreSqlArrayPredicate()
    {
        DbContextOptions<HealthcareDbContext> options =
            new DbContextOptionsBuilder<HealthcareDbContext>()
               .UseNpgsql("Host=localhost;Database=healthcare_translation_test;Username=test;Password=test")
               .Options;
        using var dbContext = new HealthcareDbContext(options);
        PatientId[] patientIds =
        [
            new PatientId(Guid.NewGuid()),
            new PatientId(Guid.NewGuid())
        ];

        string sql = dbContext.PatientCareNeeds
           .Where(careNeed => patientIds.Contains(careNeed.Id))
           .ToQueryString();

        Assert.Contains("patient_id", sql, StringComparison.Ordinal);
        Assert.Contains("ANY", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static PatientCareNeed CreateCareNeed(Guid patientId, CareNeedUrgency urgency)
    {
        return PatientCareNeed.Register(
            patientId: new PatientId(patientId),
            simulationHostId: new SimulationHostId(Guid.NewGuid()),
            urgency: urgency,
            requestedOn: new DateOnly(2048, 5, 6),
            assessmentRevision: 7,
            lifecycleRevision: 2,
            assessedAtUtc: AssessedAtUtc);
    }
}
