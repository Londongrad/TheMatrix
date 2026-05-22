using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationProgressionStateRepository cityPopulationProgressionStateRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        MarriageDomainService marriageDomainService,
        IPersonWriteRepository personWriteRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<KillPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            KillPersonCommand request,
            CancellationToken cancellationToken = default)
        {
            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();
            DateOnly today = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime);

            person.Die(today);

            CityId? cityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: person.Id,
                cancellationToken: cancellationToken);
            DateOnly currentDate = cityId is not null
                ? (await cityPopulationProgressionStateRepository.GetByCityAsync(
                      cityId: cityId.Value,
                      cancellationToken: cancellationToken))?.LastProcessedDate ??
                  today
                : today;

            if (cityId is not null && person.SpouseId is not null)
            {
                Domain.Entities.Person? spouse = await personReadRepository.FindByIdAsync(
                    id: person.SpouseId.Value,
                    cancellationToken: cancellationToken);
                CityId? spouseCityId = spouse is null
                    ? null
                    : await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                        personId: spouse.Id,
                        cancellationToken: cancellationToken);

                if (spouseCityId is not null && spouseCityId.Value == cityId.Value)
                {
                    bool spouseBecameWidowed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                        deceased: person,
                        spouse: spouse,
                        marriageDomainService: marriageDomainService);

                    if (spouseBecameWidowed && spouse is not null)
                        await cityPopulationActivityJournalService.RecordAsync(
                            entry: ClassicCityActivityFactory.ResidentBecameWidowed(
                                cityId: cityId.Value.Value,
                                currentDate: currentDate,
                                resident: spouse,
                                deceasedName: person.Name.ToString(),
                                source: CityPopulationActivitySource.Operator,
                                occurredAtUtc: occurredAtUtc),
                            cancellationToken: cancellationToken);
                }

                if (spouse is not null)
                    await personWriteRepository.UpdateAsync(
                        person: spouse,
                        cancellationToken: cancellationToken);
            }

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);

            if (cityId is not null)
            {
                await cityPopulationSummaryProjectionService.RebuildAsync(
                    cityId: cityId.Value,
                    currentDate: currentDate,
                    cancellationToken: cancellationToken);

                await cityPopulationActivityJournalService.RecordAsync(
                    entry: ClassicCityActivityFactory.ResidentDied(
                        cityId: cityId.Value.Value,
                        currentDate: currentDate,
                        resident: person,
                        source: CityPopulationActivitySource.Operator,
                        occurredAtUtc: occurredAtUtc),
                    cancellationToken: cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto(timeProvider);
        }
    }
}
