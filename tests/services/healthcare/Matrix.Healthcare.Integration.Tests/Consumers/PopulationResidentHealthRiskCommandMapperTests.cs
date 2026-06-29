using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentHealthRiskCommandMapperTests
    {
        [Fact]
        public void Map_ResidentRisk_MapsScenarioNeutralProgressionCommand()
        {
            Guid residentId = Guid.NewGuid();
            PopulationResidentHealthRiskBatchV1 message = CreateMessage(
                new PopulationResidentHealthRiskV1(
                    ResidentId: residentId,
                    EnergyScore: 45,
                    HappinessScore: 38,
                    StressScore: 72,
                    SocialNeedScore: 51,
                    IsVulnerable: true,
                    HousingStability: "Homeless",
                    HasStructuredDailyActivity: false,
                    InfectiousHouseholdContacts: 1,
                    HouseholdSize: 3,
                    CaregiverSupportStrength: 0.1d,
                    HadAdverseWeatherExposure: true,
                    HealthcareSupportStrength: 0.2d,
                    PublicHealthRiskStrength: 0.4d));

            AdvancePatientHealthCommand command = PopulationResidentHealthRiskCommandMapper.Map(message);

            AdvancePatientHealthRiskItem patient = Assert.Single(command.Patients);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(message.PreviousDate, command.PreviousDate);
            Assert.Equal(residentId, patient.PatientId);
            Assert.Equal(PatientHousingStability.Unhoused, patient.HousingStability);
            Assert.Equal(0.4d, patient.PublicHealthRiskStrength);
        }

        [Fact]
        public void Map_UnknownHousingValue_ThrowsArgumentException()
        {
            PopulationResidentHealthRiskBatchV1 message = CreateMessage(
                new PopulationResidentHealthRiskV1(
                    Guid.NewGuid(), 50, 50, 50, 50, false, "Shelter", true,
                    0, 1, 0d, false, 0d, 0d));

            Assert.Throws<ArgumentException>(() =>
                PopulationResidentHealthRiskCommandMapper.Map(message));
        }

        private static PopulationResidentHealthRiskBatchV1 CreateMessage(
            params PopulationResidentHealthRiskV1[] residents)
        {
            return new PopulationResidentHealthRiskBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 21,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "health-risk:21",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: residents);
        }
    }
}
