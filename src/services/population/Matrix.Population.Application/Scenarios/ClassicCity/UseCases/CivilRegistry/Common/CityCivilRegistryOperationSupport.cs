using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common
{
    internal static class CityCivilRegistryOperationSupport
    {
        public static async Task<Person> LoadResidentInCityAsync(
            Guid cityId,
            Guid residentId,
            IPersonReadRepository personReadRepository,
            ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
            CancellationToken cancellationToken)
        {
            Person resident = await personReadRepository.FindByIdAsync(
                    id: PersonId.From(residentId),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(residentId);

            CityId? actualCityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: resident.Id,
                cancellationToken: cancellationToken);

            if (actualCityId is null || actualCityId.Value.Value != cityId)
                throw ApplicationErrorsFactory.PersonNotAssignedToCity(
                    personId: residentId,
                    cityId: cityId);

            return resident;
        }

        public static void EnsureResidentsAreCurrentSpouses(
            Person firstResident,
            Person secondResident)
        {
            if (firstResident.SpouseId != secondResident.Id || secondResident.SpouseId != firstResident.Id)
            {
                throw ApplicationErrorsFactory.CivilRegistryResidentsAreNotCurrentSpouses(
                    firstResidentId: firstResident.Id.Value,
                    secondResidentId: secondResident.Id.Value);
            }
        }

        public static CityCivilRegistryOperationResultDto CreateResult(
            string action,
            DateTimeOffset recordedAtUtc,
            DateOnly currentDate,
            Person firstResident,
            Person secondResident,
            bool includeSpouseLinks)
        {
            return new CityCivilRegistryOperationResultDto(
                Action: action,
                RecordedAtUtc: recordedAtUtc,
                FirstResident: firstResident.ToResidentDetailsDto(
                    currentDate: currentDate,
                    currentSpouse: includeSpouseLinks ? secondResident : null),
                SecondResident: secondResident.ToResidentDetailsDto(
                    currentDate: currentDate,
                    currentSpouse: includeSpouseLinks ? firstResident : null));
        }
    }
}
