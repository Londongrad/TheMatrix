using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration;
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
        public void Create_WhenResidentHasExternalActivity_ProjectsCityTransferAndSpendingPattern()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                externalActivity: CreateExternalActivity(),
                currentDate: CurrentDate);

            Assert.Equal(
                expected: Money.FromDecimal(10m),
                actual: context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(6m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.010d, context.EmploymentOpportunityBonus);
            Assert.Equal(0d, context.EmploymentAvailabilityFactor);
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
                externalActivity: null,
                currentDate: CurrentDate);

            Assert.Equal(CityResidentEconomicContext.Neutral, context);
        }

        [Fact]
        public void Create_WhenResidentHasQualification_ProjectsEmploymentModifiersWithoutTransfer()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                externalActivity: CreateExternalActivity(
                    hasStructuredActivity: false,
                    qualification: ResidentWorkforceQualificationTier.Professional),
                currentDate: CurrentDate);

            Assert.Equal(Money.Zero, context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(14m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.024d, context.EmploymentOpportunityBonus);
            Assert.Equal(1d, context.EmploymentAvailabilityFactor);
        }

        private static ResidentExternalActivityProfile CreateExternalActivity(
            bool hasStructuredActivity = true,
            ResidentWorkforceQualificationTier qualification =
                ResidentWorkforceQualificationTier.General)
        {
            return new ResidentExternalActivityProfile(
                HasStructuredActivity: hasStructuredActivity,
                DestinationAnchorId: hasStructuredActivity ? Guid.NewGuid() : null,
                WorkforceQualification: qualification);
        }
    }
}
