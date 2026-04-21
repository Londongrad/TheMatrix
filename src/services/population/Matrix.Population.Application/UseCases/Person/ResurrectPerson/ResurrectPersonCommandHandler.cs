using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.ResurrectPerson
{
    public sealed class ResurrectPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationProgressionStateRepository cityPopulationProgressionStateRepository,
        ICityPopulationActivityJournalService cityPopulationActivityJournalService,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IPersonWriteRepository personWriteRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ResurrectPersonCommand, PersonDto>
    {
        public async Task<PersonDto> Handle(
            ResurrectPersonCommand request,
            CancellationToken cancellationToken)
        {
            Domain.Entities.Person person =
                await personReadRepository.FindByIdAsync(
                    id: PersonId.From(request.Id),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.Id);

            person.Resurrect();

            CityId? cityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: person.Id,
                cancellationToken: cancellationToken);

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);

            if (cityId is not null)
            {
                DateOnly currentDate = (await cityPopulationProgressionStateRepository.GetByCityAsync(
                                           cityId: cityId.Value,
                                           cancellationToken: cancellationToken))?.LastProcessedDate ??
                                       DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

                await cityPopulationSummaryProjectionService.RebuildAsync(
                    cityId: cityId.Value,
                    currentDate: currentDate,
                    cancellationToken: cancellationToken);

                await cityPopulationActivityJournalService.RecordAsync(
                    entry: ClassicCityActivityFactory.ResidentResurrected(
                        cityId: cityId.Value.Value,
                        currentDate: currentDate,
                        resident: person,
                        source: CityPopulationActivitySource.Operator),
                    cancellationToken: cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto(timeProvider);
        }
    }
}
