using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    internal sealed record CityOperationalSnapshot(
        CityListItemView City,
        CityEnvironmentalConditionsView? Conditions,
        CityPopulationDistrictPressureDto? PopulationDistrictPressure,
        CityDistrictHeatingConditionsView? DistrictHeating,
        CityDistrictWaterDistributionConditionsView? DistrictWater,
        CityDistrictPowerDistributionConditionsView? DistrictPower,
        CityDistrictSanitationConditionsView? DistrictSanitation,
        CityDistrictUtilityIncidentConditionsView? DistrictUtilityIncidents,
        IReadOnlyList<CityActiveTripView>? ActiveTrips,
        CityStockpilesView? Stockpiles,
        CityOperationalBudgetPressureView? Budget);
}
