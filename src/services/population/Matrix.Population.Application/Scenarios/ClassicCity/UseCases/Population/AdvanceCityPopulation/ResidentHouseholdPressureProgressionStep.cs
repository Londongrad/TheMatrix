using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentHouseholdPressureProgressionStep
    {
        internal static async Task<bool> ApplyAsync(
            CityId cityId,
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            EducationParticipationProjectionIndex educationParticipation,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            IDictionary<HouseholdId, CityHouseholdCommutePressureProfile?> commutePressureProfilesByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken,
            CityHouseholdPressurePolicy householdPressurePolicy)
        {
            if (!residentsByHouseholdId.TryGetValue(
                    key: person.HouseholdId,
                    value: out IReadOnlyCollection<PersonEntity>? householdResidents))
                return false;

            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            financialStressByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out CityPopulationHouseholdFinancialStressState? financialStressState);
            if (!commutePressureProfilesByHouseholdId.TryGetValue(
                    key: person.HouseholdId,
                    value: out CityHouseholdCommutePressureProfile? commutePressureProfile))
            {
                commutePressureProfile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                    cityId: cityId,
                    householdId: person.HouseholdId,
                    householdResidents: householdResidents,
                    educationParticipation: educationParticipation,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken);
                commutePressureProfilesByHouseholdId[person.HouseholdId] = commutePressureProfile;
            }

            return householdPressurePolicy.Apply(
                resident: person,
                householdResidents: householdResidents,
                routineProfilesByResidentId: routineProfilesByResidentId,
                housingStatus: housingStatus,
                financialStressState: financialStressState,
                commutePressureProfile: commutePressureProfile,
                previousDate: previousDate,
                currentDate: currentDate);
        }
    }
}
