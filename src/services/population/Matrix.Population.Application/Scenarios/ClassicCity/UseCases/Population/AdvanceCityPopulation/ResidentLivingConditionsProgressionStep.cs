using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using HouseholdId = Matrix.Population.Domain.ValueObjects.HouseholdId;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentLivingConditionsProgressionStep
    {
        internal static ResidentProgressionStepResult Apply(
            PersonEntity person,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByDistrictId,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy)
        {
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            DistrictId? districtId = districtByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out DistrictId? resolvedDistrictId)
                ? resolvedDistrictId
                : null;
            CityPopulationLivingConditionsContext districtLivingConditions =
                districtImpactPolicy.ResolveLivingConditions(
                    districtId: districtId,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditions: ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                        districtId: districtId,
                        districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId));
            CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: districtId,
                essentialsState: essentialsState);
            CityPopulationLivingConditionsPressureEffect effect = livingConditionsPressurePolicy.Calculate(
                person: person,
                previousDate: previousDate,
                currentDate: currentDate,
                housingStatus: housingStatus,
                livingConditions: districtLivingConditions,
                essentials: districtEssentials);

            if (!effect.HasAnyEffect)
                return ResidentProgressionStepResult.None;

            int previousEnergy = person.Energy.Value;
            int previousStress = person.Stress.Value;
            int previousHappiness = person.Happiness.Value;

            if (person.IsAlive)
            {
                if (effect.EnergyDelta != 0)
                    person.ChangeEnergy(effect.EnergyDelta);
                if (effect.StressDelta != 0)
                    person.ChangeStress(effect.StressDelta);
                if (effect.HappinessDelta != 0)
                    person.ChangeHappiness(effect.HappinessDelta);
            }

            bool changed = previousEnergy != person.Energy.Value ||
                           previousStress != person.Stress.Value ||
                           previousHappiness != person.Happiness.Value;

            return new ResidentProgressionStepResult(
                PopulationChanged: changed,
                HealthcareHealthDelta: effect.HealthDelta);
        }
    }
}
