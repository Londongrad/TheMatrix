using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration
{
    public sealed class PopulationResidentMedicalStateBatchFactoryTests
    {
        [Fact]
        public void Build_OrdersChunksAndPreservesMedicalState()
        {
            Person third = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                health: 63);
            Person first = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Person second = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            DateOnly diagnosedOn = new(2048, 5, 1);
            third.Die(diagnosedOn.AddDays(-2));
            third.Resurrect();
            ApplyHealthcareProjection(
                person: third,
                currentDate: diagnosedOn,
                illnessKind: IllnessKind.Infection,
                illnessSeverity: IllnessSeverity.Moderate,
                diagnosedOn: diagnosedOn,
                healthScore: 63);
            Guid hostId = Guid.NewGuid();

            PopulationResidentMedicalStateBatchV1[] batches =
                PopulationResidentMedicalStateBatchFactory.Build(
                    simulationHostId: hostId,
                    sourceRevision: 42,
                    residents: new[] { third, first, second },
                    correlationId: "population:host:tick:42:medical-state",
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

            PopulationResidentMedicalStateV1 sick = batches
               .SelectMany(batch => batch.Residents)
               .Single(state => state.ResidentId == third.Id.Value);
            Assert.Equal(63, sick.HealthScore);
            Assert.Equal("Infection", sick.CurrentIllnessKind);
            Assert.Equal("Moderate", sick.CurrentIllnessSeverity);
            Assert.Equal(diagnosedOn, sick.DiagnosedOn);
            Assert.Equal(2, sick.LifecycleRevision);
        }

        [Fact]
        public void Build_EmptyResidents_ReturnsNoBatches()
        {
            PopulationResidentMedicalStateBatchV1[] batches =
                PopulationResidentMedicalStateBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    sourceRevision: 0,
                    residents: Array.Empty<Person>(),
                    correlationId: "population:host:medical-state",
                    observedAtUtc: UtcNow);

            Assert.Empty(batches);
        }
    }
}
