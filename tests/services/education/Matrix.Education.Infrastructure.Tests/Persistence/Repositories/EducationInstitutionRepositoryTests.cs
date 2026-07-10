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
            IReadOnlyList<EducationInstitution> listed = await repository.ListAsync(simulationHostId);

            Assert.NotNull(loaded);
            Assert.Equal(first.EducationInstitutionId, loaded.EducationInstitutionId);
            Assert.Equal(new[] { "Academy", "University" }, listed.Select(item => item.Name));
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
