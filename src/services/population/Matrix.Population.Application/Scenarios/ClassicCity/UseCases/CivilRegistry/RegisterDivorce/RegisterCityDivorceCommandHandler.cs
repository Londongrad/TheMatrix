using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce
{
    public sealed class RegisterCityDivorceCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository,
        IPersonWriteRepository personWriteRepository,
        IHouseholdWriteRepository householdWriteRepository,
        MarriageDomainService marriageDomainService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RegisterCityDivorceCommand, CityCivilRegistryOperationResultDto>
    {
        public async Task<CityCivilRegistryOperationResultDto> Handle(
            RegisterCityDivorceCommand request,
            CancellationToken cancellationToken)
        {
            Person firstResident = await CityCivilRegistryOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.FirstResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            Person secondResident = await CityCivilRegistryOperationSupport.LoadResidentInCityAsync(
                cityId: request.CityId,
                residentId: request.SecondResidentId,
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cancellationToken: cancellationToken);

            CityCivilRegistryOperationSupport.EnsureResidentsAreCurrentSpouses(
                firstResident: firstResident,
                secondResident: secondResident);

            marriageDomainService.RegisterDivorce(
                person: firstResident,
                spouse: secondResident,
                currentDate: request.CurrentDate);

            DateTimeOffset recordedAtUtc = timeProvider.GetUtcNow();

            await ClassicCityCivilRegistryHouseholdSupport.SeparateDivorcedSpousesAsync(
                cityId: CityId.From(request.CityId),
                firstResident: firstResident,
                secondResident: secondResident,
                householdWriteRepository: householdWriteRepository,
                createdAtUtc: recordedAtUtc,
                cancellationToken: cancellationToken);

            await personWriteRepository.UpdateAsync(
                person: firstResident,
                cancellationToken: cancellationToken);
            await personWriteRepository.UpdateAsync(
                person: secondResident,
                cancellationToken: cancellationToken);

            await cityPopulationSummaryProjectionService.RebuildAsync(
                cityId: CityId.From(request.CityId),
                currentDate: request.CurrentDate,
                cancellationToken: cancellationToken);

            await cityPopulationActivityJournalService.RecordAsync(
                entry: ClassicCityActivityFactory.ResidentsDivorced(
                    cityId: request.CityId,
                    currentDate: request.CurrentDate,
                    firstResident: firstResident,
                    secondResident: secondResident,
                    source: CityPopulationActivitySource.Operator,
                    occurredAtUtc: recordedAtUtc),
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            CityResidentHousingSnapshot? firstHousing =
                await cityPopulationPersonReadRepository.FindHousingSnapshotByPersonIdAsync(
                    cityId: CityId.From(request.CityId),
                    personId: firstResident.Id,
                    cancellationToken: cancellationToken);
            CityResidentHousingSnapshot? secondHousing =
                await cityPopulationPersonReadRepository.FindHousingSnapshotByPersonIdAsync(
                    cityId: CityId.From(request.CityId),
                    personId: secondResident.Id,
                    cancellationToken: cancellationToken);

            return await CityCivilRegistryOperationSupport.CreateResultAsync(
                action: "DivorceRegistered",
                recordedAtUtc: recordedAtUtc,
                cityId: request.CityId,
                currentDate: request.CurrentDate,
                firstResident: firstResident,
                secondResident: secondResident,
                includeSpouseLinks: false,
                firstHousing: firstHousing,
                secondHousing: secondHousing,
                educationParticipationProjectionRepository: educationParticipationProjectionRepository,
                cancellationToken: cancellationToken);
        }
    }
}
