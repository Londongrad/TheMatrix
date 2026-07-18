using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class CityResidentEconomicContextFactoryTests
    {
        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 3);

        [Fact]
        public void Create_WhenResidentIsEnrolled_ProjectsCityTransferAndSpendingPattern()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                educationParticipation: CreateEducationParticipation(resident),
                currentDate: CurrentDate);

            Assert.Equal(
                expected: Money.FromDecimal(10m),
                actual: context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(6m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.010d, context.EmploymentOpportunityBonus);
            Assert.Equal(-0.03m, context.RetailStoreSpendShareAdjustment);
            Assert.Equal(-0.01m, context.ServiceSpendShareAdjustment);
            Assert.Equal(0.04m, context.MunicipalSpendShareAdjustment);
        }

        [Fact]
        public void Create_WhenResidentIsNotEnrolled_ReturnsNeutralContext()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                educationParticipation: null,
                currentDate: CurrentDate);

            Assert.Equal(CityResidentEconomicContext.Neutral, context);
        }

        [Fact]
        public void Create_WhenResidentCompletedEducation_ProjectsEmploymentModifiersWithoutTransfer()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                educationParticipation: CreateEducationParticipation(
                    resident: resident,
                    isEnrolled: false,
                    completedStage: "higher"),
                currentDate: CurrentDate);

            Assert.Equal(Money.Zero, context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(14m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.024d, context.EmploymentOpportunityBonus);
        }

        private static EducationParticipationProjection CreateEducationParticipation(
            Person resident,
            bool isEnrolled = true,
            string? completedStage = "upper-secondary")
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
                EnrolledOn: isEnrolled ? CurrentDate.AddYears(-1) : null,
                CompletedStage: completedStage,
                CompletedStageOn: completedStage is null ? null : CurrentDate.AddYears(-2),
                SnapshotDate: CurrentDate,
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
