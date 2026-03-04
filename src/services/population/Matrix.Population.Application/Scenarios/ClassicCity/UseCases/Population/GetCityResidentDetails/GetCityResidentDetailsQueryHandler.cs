using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed class GetCityResidentDetailsQueryHandler(
        ICityPopulationPersonReadRepository cityPopulationPersonReadRepository,
        IPersonReadRepository personReadRepository)
        : IRequestHandler<GetCityResidentDetailsQuery, CityResidentDetailsDto>
    {
        public async Task<CityResidentDetailsDto> Handle(
            GetCityResidentDetailsQuery request,
            CancellationToken cancellationToken)
        {
            Person resident = await cityPopulationPersonReadRepository.FindByCityAndPersonIdAsync(
                    cityId: CityId.From(request.CityId),
                    personId: PersonId.From(request.PersonId),
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.PersonNotFound(request.PersonId);

            Person? currentSpouse = resident.SpouseId is not { } spouseId
                ? null
                : await personReadRepository.FindByIdAsync(
                    id: spouseId,
                    cancellationToken: cancellationToken);

            return resident.ToResidentDetailsDto(
                currentDate: request.CurrentDate,
                currentSpouse: currentSpouse);
        }
    }
}
