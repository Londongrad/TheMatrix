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
                LifecycleRevision = 5,
                CommunityId = communityId
            };

            PopulationResidentHealthRiskBatchV2[] batches =
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
            PopulationResidentHealthRiskV2 firstRisk = batches[0].Residents[0];
            Assert.Equal("Unhoused", firstRisk.HousingStability);
            Assert.Equal(5, firstRisk.LifecycleRevision);
            Assert.Equal(communityId, firstRisk.CommunityId);
        }

        [Fact]
        public void Build_EmptySnapshots_ReturnsNoBatches()
        {
            PopulationResidentHealthRiskBatchV2[] batches =
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

        [Fact]
        public void Build_MapsRawHealthContextsWithoutMedicalInterpretation()
        {
            Guid residentId = Guid.NewGuid();
            PopulationResidentHealthRiskSnapshot snapshot = Create(residentId) with
            {
                FunctionalCapacityScore = 64,
                IsEmployed = true,
                LifecycleRevision = 7
            };

            PopulationResidentHealthRiskBatchV2[] batches =
                PopulationResidentHealthRiskBatchFactory.Build(
                    simulationHostId: Guid.NewGuid(),
                    sourceRevision: 43,
                    previousDate: new DateOnly(2048, 5, 5),
                    currentDate: new DateOnly(2048, 5, 6),
                    residents: [snapshot],
                    correlationId: "population:host:tick:43:health-risk",
                    observedAtUtc: ObservedAtUtc);

            PopulationResidentHealthRiskV2 risk = Assert.Single(Assert.Single(batches).Residents);
            Assert.Equal(residentId, risk.ResidentId);
            Assert.Equal(64, risk.FunctionalCapacityScore);
            Assert.True(risk.IsEmployed);
            Assert.Equal(0.7d, risk.Household.StabilityScore);
            Assert.Equal(0.9d, risk.HealthcareAccess.RouteAccessibilityIndex);
            Assert.Equal(0.2d, risk.Environment.MedicineShortageRiskIndex);
            Assert.Equal(7, risk.LifecycleRevision);
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
                HouseholdSize: 3,
                CaregiverSupportStrength: 0.12d,
                HadAdverseWeatherExposure: false,
                Household: new PopulationResidentHouseholdHealthSnapshot(
                    StabilityScore: 0.7d,
                    AdultProviderCount: 1,
                    AdultStructuredParticipantCount: 1,
                    FunctionalLimitationCount: 0,
                    HasStructuredSupport: true),
                HealthcareAccess: new PopulationResidentHealthcareAccessSnapshot(
                    HasPrimaryCareDestination: true,
                    IsPrimaryCareInCommunity: true,
                    HasRouteData: true,
                    IsRouteAccessible: true,
                    RouteAccessibilityIndex: 0.9d,
                    RoutePassabilityIndex: 0.8d,
                    EstimatedTravelTimeMinutes: 15d,
                    HasInfrastructureData: true,
                    UtilityIncidentDispatchReadinessIndex: 0.9d,
                    UtilityIncidentPressureIndex: 0.1d,
                    UtilityIncidentCoordinationDifficultyIndex: 0.1d,
                    UtilityIncidentRestorationPriorityIndex: 0.2d,
                    PowerCoverageIndex: 0.95d,
                    WaterCoverageIndex: 0.9d,
                    HeatingCoverageIndex: 0.85d,
                    SanitationCoverageIndex: 0.9d,
                    HealthcareQualityIndex: 1.1d,
                    RecoverySupportIndex: 1d,
                    TriagePressureIndex: 0.2d),
                Environment: new PopulationResidentEnvironmentalHealthSnapshot(
                    WaterCoverageIndex: 0.9d,
                    SanitationCoverageIndex: 0.9d,
                    FloodingIndex: 0.1d,
                    UtilityContinuityIndex: 0.9d,
                    EmergencyWaterShortageRiskIndex: 0.1d,
                    FoodShortageRiskIndex: 0.1d,
                    MedicineShortageRiskIndex: 0.2d,
                    EmergencyRationingEnabled: false));
    }
}
