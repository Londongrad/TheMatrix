using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class CityResidentEconomicContextFactory
    {
        public static CityResidentEconomicContext Create(
            Person resident,
            EducationParticipationProjection? educationParticipation,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);

            if (educationParticipation?.IsEnrolled != true)
                return CityResidentEconomicContext.Neutral;

            decimal dailyTransferIncome = resident.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior
                ? 10m
                : 4m;

            return CityResidentEconomicContext.Create(
                dailyTransferIncome: Money.FromDecimal(dailyTransferIncome),
                retailStoreSpendShareAdjustment: -0.03m,
                serviceSpendShareAdjustment: -0.01m,
                municipalSpendShareAdjustment: 0.04m);
        }
    }
}
