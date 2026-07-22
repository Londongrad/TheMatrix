using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.Entities;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Integration.Education
{
    public sealed class EducationEconomicProjectionTests
    {
        [Theory]
        [InlineData(6, 4)]
        [InlineData(16, 4)]
        [InlineData(17, 10)]
        [InlineData(65, 10)]
        [InlineData(66, 10)]
        public void AdapterToCityProjection_PreservesStudentSupport(int age, decimal dailyIncome)
        {
            var currentDate = new DateOnly(2048, 5, 3);
            Person resident = CreatePerson(birthDate: currentDate.AddYears(-age), currentDate: currentDate);
            var participation = CreateParticipation(resident, currentDate, isEnrolled: true);
            var activity = EducationResidentExternalActivityProfileFactory.Create(participation);

            var economics = CityResidentEconomicContextFactory.Create(resident, activity, currentDate);

            Assert.Equal(Money.FromDecimal(dailyIncome), economics.DailyTransferIncome);
            Assert.Equal(Money.FromDecimal(6m), economics.EmploymentIncomeBonus);
            Assert.Equal(0.010d, economics.EmploymentOpportunityBonus);
            Assert.Equal(0d, economics.EmploymentAvailabilityFactor);
            Assert.Equal(-0.03m, economics.RetailStoreSpendShareAdjustment);
            Assert.Equal(-0.01m, economics.ServiceSpendShareAdjustment);
            Assert.Equal(0.04m, economics.MunicipalSpendShareAdjustment);
        }

        [Fact]
        public void LeapDayBirthday_ChangesIncomeWithoutRefreshingParticipation()
        {
            var birthday = new DateOnly(2049, 2, 28);
            Person resident = CreatePerson(birthDate: new DateOnly(2032, 2, 29), currentDate: birthday);
            var participation = CreateParticipation(resident, birthday.AddDays(-1), isEnrolled: true);
            var activity = EducationResidentExternalActivityProfileFactory.Create(participation);

            Assert.Equal(Money.FromDecimal(4m),
                CityResidentEconomicContextFactory.Create(resident, activity, birthday.AddDays(-1)).DailyTransferIncome);
            Assert.Equal(Money.FromDecimal(10m),
                CityResidentEconomicContextFactory.Create(resident, activity, birthday).DailyTransferIncome);
        }

        [Fact]
        public void Withdrawal_RemovesSupportButPreservesCompletedStageEffects()
        {
            var currentDate = new DateOnly(2048, 5, 3);
            Person resident = CreatePerson(currentDate: currentDate);
            var participation = CreateParticipation(resident, currentDate, isEnrolled: false);
            var activity = EducationResidentExternalActivityProfileFactory.Create(participation);

            var economics = CityResidentEconomicContextFactory.Create(resident, activity, currentDate);

            Assert.Equal(Money.Zero, economics.DailyTransferIncome);
            Assert.Equal(Money.FromDecimal(6m), economics.EmploymentIncomeBonus);
            Assert.Equal(0.010d, economics.EmploymentOpportunityBonus);
            Assert.Equal(1d, economics.EmploymentAvailabilityFactor);
            Assert.Equal(0m, economics.RetailStoreSpendShareAdjustment);
            Assert.Equal(0m, economics.ServiceSpendShareAdjustment);
            Assert.Equal(0m, economics.MunicipalSpendShareAdjustment);
        }

        private static EducationParticipationProjection CreateParticipation(
            Person resident, DateOnly snapshotDate, bool isEnrolled)
        {
            return new EducationParticipationProjection(
                SimulationHostId: Guid.NewGuid(),
                ResidentId: resident.Id.Value,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: resident.LifecycleRevision,
                IsEnrolled: isEnrolled,
                ActiveStage: isEnrolled ? "higher" : null,
                InstitutionId: isEnrolled ? Guid.NewGuid() : null,
                InstitutionAnchorId: isEnrolled ? Guid.NewGuid() : null,
                EnrolledOn: isEnrolled ? snapshotDate : null,
                CompletedStage: "upper-secondary",
                CompletedStageOn: snapshotDate.AddDays(-1),
                SnapshotDate: snapshotDate,
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
