using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Models;
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
        public void Create_WhenProviderSuppliesTerms_ProjectsThemWithoutEducationDefaults()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                externalActivity: CreateExternalActivity(economics: CreateEconomicTerms()),
                currentDate: CurrentDate);

            Assert.Equal(
                expected: Money.FromDecimal(13m),
                actual: context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(8m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.04d, context.EmploymentOpportunityBonus);
            Assert.Equal(0.6d, context.EmploymentAvailabilityFactor);
            Assert.Equal(-0.07m, context.RetailStoreSpendShareAdjustment);
            Assert.Equal(0.02m, context.ServiceSpendShareAdjustment);
            Assert.Equal(0.05m, context.MunicipalSpendShareAdjustment);
        }

        [Fact]
        public void Create_WhenNoExternalProfile_ReturnsNeutralContext()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                externalActivity: null,
                currentDate: CurrentDate);

            Assert.Equal(CityResidentEconomicContext.Neutral, context);
        }

        [Fact]
        public void Create_WithoutRoutine_StillAppliesExplicitEmploymentTerms()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);

            CityResidentEconomicContext context = CityResidentEconomicContextFactory.Create(
                resident: resident,
                externalActivity: CreateExternalActivity(
                    hasStructuredActivity: false,
                    economics: new ResidentExternalEconomicProfile(
                        ResidentAgeIncomeSchedule.None, 11m, 0.03d)),
                currentDate: CurrentDate);

            Assert.Equal(Money.Zero, context.DailyTransferIncome);
            Assert.Equal(
                expected: Money.FromDecimal(11m),
                actual: context.EmploymentIncomeBonus);
            Assert.Equal(0.03d, context.EmploymentOpportunityBonus);
            Assert.Equal(1d, context.EmploymentAvailabilityFactor);
        }

        [Fact]
        public void Create_AfterResurrection_RejectsPreviousLifecycleQualification()
        {
            Person resident = CreatePerson();
            ResidentExternalActivityProfile activity = CreateExternalActivity(economics: CreateEconomicTerms());
            resident.Die(CurrentDate);
            resident.Resurrect();

            Assert.Same(
                CityResidentEconomicContext.Neutral,
                CityResidentEconomicContextFactory.Create(resident, activity, CurrentDate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_WithoutEconomicTerms_DoesNotInferBenefitsFromActivityOrQualification(bool hasActivity)
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            ResidentExternalActivityProfile activity = CreateExternalActivity(hasStructuredActivity: hasActivity);

            Assert.Same(CityResidentEconomicContext.Neutral,
                CityResidentEconomicContextFactory.Create(resident, activity, CurrentDate));
        }

        [Fact]
        public void Create_OnBirthday_UsesNewBandFromSameProviderSnapshot()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            ResidentExternalActivityProfile activity = CreateExternalActivity(economics: CreateEconomicTerms());

            var beforeBirthday = CityResidentEconomicContextFactory.Create(resident, activity, CurrentDate.AddDays(-1));
            var onBirthday = CityResidentEconomicContextFactory.Create(resident, activity, CurrentDate);

            Assert.Equal(Money.FromDecimal(2m), beforeBirthday.DailyTransferIncome);
            Assert.Equal(Money.FromDecimal(13m), onBirthday.DailyTransferIncome);
        }

        private static ResidentExternalEconomicProfile CreateEconomicTerms()
        {
            return new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.Create((0, 2m), (18, 13m)),
                8m, 0.04d, 0.6d, -0.07m, 0.02m, 0.05m);
        }

        private static ResidentExternalActivityProfile CreateExternalActivity(
            bool hasStructuredActivity = true,
            ResidentExternalEconomicProfile? economics = null)
        {
            return new ResidentExternalActivityProfile(
                ResidentLifecycleRevision: 0,
                Routine: hasStructuredActivity
                    ? PersonRoutineProfile.Structured(
                        activityStart: TimeSpan.FromHours(8),
                        activityEnd: TimeSpan.FromHours(15),
                        activityLoad: PersonStructuredActivityLoad.Moderate)
                    : PersonRoutineProfile.Unstructured,
                DestinationAnchorId: hasStructuredActivity ? Guid.NewGuid() : null,
                CommutePurpose: hasStructuredActivity ? "TestActivityCommute" : null,
                WorkforceQualification: ResidentWorkforceQualificationTier.General,
                Economics: economics);
        }
    }
}
