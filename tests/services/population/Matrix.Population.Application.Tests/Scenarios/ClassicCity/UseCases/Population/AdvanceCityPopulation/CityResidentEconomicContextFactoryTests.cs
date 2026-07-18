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

        private static EducationParticipationProjection CreateEducationParticipation(Person resident)
        {
            return new EducationParticipationProjection(
                SimulationHostId: Guid.NewGuid(),
                ResidentId: resident.Id.Value,
                ParticipationRevision: 1,
                ResidentLifecycleRevision: resident.LifecycleRevision,
                IsEnrolled: true,
                ActiveStage: "higher",
                InstitutionId: Guid.NewGuid(),
                InstitutionAnchorId: Guid.NewGuid(),
                EnrolledOn: CurrentDate.AddYears(-1),
                CompletedStage: "upper-secondary",
                CompletedStageOn: CurrentDate.AddYears(-2),
                SnapshotDate: CurrentDate,
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
