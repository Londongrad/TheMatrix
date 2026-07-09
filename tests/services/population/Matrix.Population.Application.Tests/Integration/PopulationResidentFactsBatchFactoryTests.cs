using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration
{
    public sealed class PopulationResidentFactsBatchFactoryTests
    {
        [Fact]
        public void Build_OrdersAndChunksScenarioNeutralResidentFacts()
        {
            Guid hostId = Guid.NewGuid();
            DateTimeOffset synchronizedAtUtc = UtcNow;
            Person third = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                sex: Sex.Female,
                lifeStatus: LifeStatus.Deceased,
                birthDate: new DateOnly(2028, 3, 4));
            Person first = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                sex: Sex.Male,
                birthDate: new DateOnly(2030, 1, 2));
            Person second = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                sex: Sex.Female,
                birthDate: new DateOnly(2029, 2, 3));
            third.Resurrect();
            third.Die(new DateOnly(2048, 5, 1));

            PopulationResidentFactsBatchV1[] batches = PopulationResidentFactsBatchFactory.Build(
                simulationHostId: hostId,
                sourceRevision: 42,
                residents: new[] { third, first, second },
                correlationId: "population:host:tick:42:resident-facts",
                synchronizedAtUtc: synchronizedAtUtc,
                batchSize: 2);

            Assert.Equal(2, batches.Length);
            Assert.All(batches, batch =>
            {
                Assert.Equal(hostId, batch.SimulationHostId);
                Assert.Equal(42, batch.SourceRevision);
                Assert.Equal(synchronizedAtUtc, batch.SynchronizedAtUtc);
                Assert.Equal(2, batch.TotalBatches);
            });
            Assert.Equal(1, batches[0].BatchNumber);
            Assert.Equal(2, batches[1].BatchNumber);
            Assert.Equal(
                new[] { first.Id.Value, second.Id.Value, third.Id.Value },
                batches.SelectMany(batch => batch.Residents).Select(fact => fact.ResidentId));

            PopulationResidentFactsV1 deceased = batches
               .SelectMany(batch => batch.Residents)
               .Single(fact => fact.ResidentId == third.Id.Value);
            Assert.Equal("Female", deceased.Sex);
            Assert.Equal(third.BirthDate, deceased.BirthDate);
            Assert.False(deceased.IsAlive);
            Assert.True(deceased.IsActive);
            Assert.Equal(3, deceased.LifecycleRevision);
            Assert.Equal(third.HouseholdId.Value, deceased.HouseholdId);
        }

        [Fact]
        public void Build_EmptyResidents_ReturnsNoBatches()
        {
            PopulationResidentFactsBatchV1[] batches = PopulationResidentFactsBatchFactory.Build(
                simulationHostId: Guid.NewGuid(),
                sourceRevision: 0,
                residents: Array.Empty<Person>(),
                correlationId: "population:host:bootstrap:resident-facts",
                synchronizedAtUtc: UtcNow);

            Assert.Empty(batches);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        public void Build_InvalidBatchSize_Throws(int batchSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PopulationResidentFactsBatchFactory.Build(
                simulationHostId: Guid.NewGuid(),
                sourceRevision: 0,
                residents: new[] { CreatePerson() },
                correlationId: "population:host:resident-facts",
                synchronizedAtUtc: UtcNow,
                batchSize: batchSize));
        }

        [Fact]
        public void Build_NonUtcTimestamp_Throws()
        {
            Assert.Throws<ArgumentException>(() => PopulationResidentFactsBatchFactory.Build(
                simulationHostId: Guid.NewGuid(),
                sourceRevision: 0,
                residents: new[] { CreatePerson() },
                correlationId: "population:host:resident-facts",
                synchronizedAtUtc: UtcNow.ToOffset(TimeSpan.FromHours(3))));
        }
    }
}
