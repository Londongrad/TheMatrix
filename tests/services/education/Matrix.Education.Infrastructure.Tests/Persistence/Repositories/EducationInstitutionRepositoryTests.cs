using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class EducationInstitutionRepositoryTests
    {
        [Fact]
        public async Task AddGetAndList_ScopeInstitutionsToSimulationHost()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new EducationInstitutionRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            EducationInstitution first = CreateInstitution(simulationHostId, "Academy");
            EducationInstitution second = CreateInstitution(simulationHostId, "University");
            EducationInstitution foreign = CreateInstitution(
                new SimulationHostId(Guid.NewGuid()),
                "Foreign school");

            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await repository.AddAsync(foreign);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            EducationInstitution? loaded = await repository.GetAsync(
                simulationHostId,
                first.EducationInstitutionId);
            IReadOnlyList<EducationInstitution> selected = await repository.GetByIdsAsync(
                simulationHostId,
                [second.EducationInstitutionId, foreign.EducationInstitutionId]);
            IReadOnlyList<EducationInstitution> listed = await repository.ListAsync(simulationHostId);

            Assert.NotNull(loaded);
            Assert.Equal(first.EducationInstitutionId, loaded.EducationInstitutionId);
            Assert.Equal(second.EducationInstitutionId, Assert.Single(selected).EducationInstitutionId);
            Assert.Equal(new[] { "Academy", "University" }, listed.Select(item => item.Name));
        }

        [Fact]
        public async Task ListActiveAsync_FiltersInactiveAndForeignInstitutions()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new EducationInstitutionRepository(dbContext);
            var simulationHostId = new SimulationHostId(Guid.NewGuid());
            EducationInstitution academy = CreateInstitution(simulationHostId, "Academy");
            EducationInstitution university = CreateInstitution(simulationHostId, "University");
            EducationInstitution inactive = CreateInstitution(simulationHostId, "Inactive school");
            inactive.Deactivate();
            EducationInstitution foreign = CreateInstitution(
                new SimulationHostId(Guid.NewGuid()),
                "Foreign school");

            await repository.AddRangeAsync([university, inactive, foreign, academy]);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            IReadOnlyList<EducationInstitution> listed =
                await repository.ListActiveAsync(simulationHostId);

            Assert.Equal(
                expected: ["Academy", "University"],
                actual: listed.Select(item => item.Name));
            Assert.All(
                collection: listed,
                action: institution => Assert.True(institution.IsActive));
            Assert.Empty(dbContext.ChangeTracker.Entries());
        }

        private static EducationInstitution CreateInstitution(
            SimulationHostId simulationHostId,
            string name)
        {
            return EducationInstitution.Create(
                id: new EducationInstitutionId(Guid.NewGuid()),
                simulationHostId: simulationHostId,
                name: name,
                kind: new EducationInstitutionKindKey("general"),
                capacity: 100);
        }
    }
}
