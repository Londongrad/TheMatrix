using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryHandler(ICityPopulationPersonReadRepository personReadRepository)
        : IRequestHandler<GetCityResidentsPageQuery, PagedResult<PersonDto>>
    {
        public async Task<PagedResult<PersonDto>> Handle(
            GetCityResidentsPageQuery request,
            CancellationToken cancellationToken)
        {
            (IReadOnlyCollection<Person> persons, int totalCount) = await personReadRepository.GetPageByCityAsync(
                cityId: CityId.From(request.CityId),
                pagination: request.Pagination,
                cancellationToken: cancellationToken);

            IReadOnlyCollection<PersonDto> dtos = persons.ToDtoCollection(request.CurrentDate);

            return new PagedResult<PersonDto>(
                items: dtos,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }
    }
}
