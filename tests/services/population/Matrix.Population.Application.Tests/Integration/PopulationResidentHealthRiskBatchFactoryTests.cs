using Matrix.Population.Application.Integration;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Population.Application.Tests.Integration
{
    public sealed class PopulationResidentHealthRiskBatchFactoryTests
    {
        private static readonly DateTimeOffset ObservedAtUtc =
            new(2048, 5, 6, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Build_OrdersAndChunksPreparedRiskSnapshots()
        {
            Guid firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            Guid thirdId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            Guid communityId = Guid.NewGuid();
            PopulationResidentHealthRiskSnapshot first = Create(firstId) with
            {
                HousingStability = "Unhoused",
                InfectiousHouseholdContacts = 2,
                HealthcareSupportStrength = 0.42d,
                LifecycleRevision = 5,
                CommunityId = communityId
            };

            PopulationResidentHealthRiskBatchV1[] batches =
                PopulationResidentHealthRiskBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    sourceRevision: 42,
                    previousDate: new DateOnly(2048, 5, 5),
                    currentDate: new DateOnly(2048, 5, 6),
                    residents: [Create(thirdId), first, Create(secondId)],
                    correlationId: "population:host:tick:42:health-risk",
                    observedAtUtc: ObservedAtUtc,
                    batchSize: 2);

            Assert.Equal(2, batches.Length);
            Assert.All(batches, batch => Assert.Equal(2, batch.TotalBatches));
            Assert.Equal(
                new[] { firstId, secondId, thirdId },
                batches.SelectMany(batch => batch.Residents).Select(risk => risk.ResidentId));
            PopulationResidentHealthRiskV1 firstRisk = batches[0].Residents[0];
            Assert.Equal("Unhoused", firstRisk.HousingStability);
            Assert.Equal(2, firstRisk.InfectiousHouseholdContacts);
            Assert.Equal(0.42d, firstRisk.HealthcareSupportStrength);
            Assert.Equal(5, firstRisk.LifecycleRevision);
            Assert.Equal(communityId, firstRisk.CommunityId);
        }

        [Fact]
        public void Build_EmptySnapshots_ReturnsNoBatches()
        {
            PopulationResidentHealthRiskBatchV1[] batches =
                PopulationResidentHealthRiskBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    sourceRevision: 0,
                    previousDate: new DateOnly(2048, 5, 5),
                    currentDate: new DateOnly(2048, 5, 6),
                    residents: Array.Empty<PopulationResidentHealthRiskSnapshot>(),
                    correlationId: "population:host:tick:0:health-risk",
                    observedAtUtc: ObservedAtUtc);

            Assert.Empty(batches);
        }

        private static PopulationResidentHealthRiskSnapshot Create(Guid residentId) =>
            new(
                ResidentId: residentId,
                EnergyScore: 61,
                HappinessScore: 62,
                StressScore: 38,
                SocialNeedScore: 23,
                IsVulnerable: false,
                HousingStability: "Housed",
                HasStructuredDailyActivity: true,
                InfectiousHouseholdContacts: 0,
                HouseholdSize: 3,
                CaregiverSupportStrength: 0.12d,
                HadAdverseWeatherExposure: false,
                HealthcareSupportStrength: 0.51d,
                PublicHealthRiskStrength: 0.17d);
    }
}
