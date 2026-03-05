using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public class KillPersonCommandHandler(
        IPersonReadRepository personReadRepository,
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        ICityPopulationProgressionStateRepository cityPopulationProgressionStateRepository,
        ICityPopulationSummaryProjectionService cityPopulationSummaryProjectionService,
        IPersonWriteRepository personWriteRepository,
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

            person.Die(DateOnly.FromDateTime(DateTime.UtcNow));

            CityId? cityId = await cityPopulationPersonReadRepository.FindCityIdByPersonIdAsync(
                personId: person.Id,
                cancellationToken: cancellationToken);

            await personWriteRepository.UpdateAsync(
                person: person,
                cancellationToken: cancellationToken);

            if (cityId is not null)
            {
                var currentDate = (await cityPopulationProgressionStateRepository.GetByCityAsync(
                    cityId: cityId.Value,
                    cancellationToken: cancellationToken))?.LastProcessedDate ??
                                  DateOnly.FromDateTime(DateTime.UtcNow);

                await cityPopulationSummaryProjectionService.RebuildAsync(
                    cityId: cityId.Value,
                    currentDate: currentDate,
                    cancellationToken: cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return person.ToDto();
        }
    }
}
