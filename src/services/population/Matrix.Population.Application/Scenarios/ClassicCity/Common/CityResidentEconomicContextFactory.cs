using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class CityResidentEconomicContextFactory
    {
        public static CityResidentEconomicContext Create(
            Person resident,
            ResidentExternalActivityProfile? externalActivity,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);

            if (externalActivity?.ResidentLifecycleRevision != resident.LifecycleRevision)
                return CityResidentEconomicContext.Neutral;

            ResidentExternalEconomicProfile economics = externalActivity.Economics;
            if (economics == ResidentExternalEconomicProfile.Neutral)
                return CityResidentEconomicContext.Neutral;

            return CityResidentEconomicContext.Create(
                dailyTransferIncome: Money.FromDecimal(
                    economics.TransferIncome.Resolve(resident.GetAge(currentDate).Years)),
                employmentIncomeBonus: Money.FromDecimal(economics.EmploymentIncomeBonus),
                employmentOpportunityBonus: economics.EmploymentOpportunityBonus,
                employmentAvailabilityFactor: economics.EmploymentAvailabilityFactor,
                retailStoreSpendShareAdjustment: economics.RetailStoreSpendShareAdjustment,
                serviceSpendShareAdjustment: economics.ServiceSpendShareAdjustment,
                municipalSpendShareAdjustment: economics.MunicipalSpendShareAdjustment);
        }
    }
}
