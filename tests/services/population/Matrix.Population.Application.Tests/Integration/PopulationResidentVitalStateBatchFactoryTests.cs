using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration
{
    public sealed class PopulationResidentVitalStateBatchFactoryTests
    {
        [Fact]
        public void Build_OrdersChunksAndPreservesOnlyUniversalVitalState()
        {
            Person third = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                health: 63);
            Person first = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Person second = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            third.Die(new DateOnly(2048, 4, 29));
            third.Resurrect();
            Guid hostId = Guid.NewGuid();

            PopulationResidentVitalStateBatchV1[] batches =
                PopulationResidentVitalStateBatchFactory.Build(
                    simulationHostId: hostId,
                    sourceRevision: 42,
                    residents: new[] { third, first, second },
                    correlationId: "population:host:tick:42:vital-state",
                    observedAtUtc: UtcNow,
                    batchSize: 2);

            Assert.Equal(2, batches.Length);
            Assert.All(batches, batch =>
            {
                Assert.Equal(hostId, batch.SimulationHostId);
                Assert.Equal(42, batch.SourceRevision);
                Assert.Equal(2, batch.TotalBatches);
            });
            Assert.Equal(
                new[] { first.Id.Value, second.Id.Value, third.Id.Value },
                batches.SelectMany(batch => batch.Residents).Select(state => state.ResidentId));

            PopulationResidentVitalStateV1 vitalState = batches
               .SelectMany(batch => batch.Residents)
               .Single(state => state.ResidentId == third.Id.Value);
            Assert.Equal(100, vitalState.HealthScore);
            Assert.Equal(2, vitalState.LifecycleRevision);
        }

        [Fact]
        public void Build_EmptyResidents_ReturnsNoBatches()
        {
            PopulationResidentVitalStateBatchV1[] batches =
                PopulationResidentVitalStateBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    sourceRevision: 0,
                    residents: Array.Empty<Person>(),
                    correlationId: "population:host:vital-state",
                    observedAtUtc: UtcNow);

            Assert.Empty(batches);
        }
    }
}
