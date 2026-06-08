using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using MediatR;
using DomainPerson = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.UseCases.Population.GetPeoplePage;

public sealed class GetPeoplePageQueryHandler(
    IPersonReadRepository personReadRepository,
    TimeProvider timeProvider)
    : IRequestHandler<GetPeoplePageQuery, PagedResult<PersonDto>>
{
    public async Task<PagedResult<PersonDto>> Handle(
        GetPeoplePageQuery request,
        CancellationToken cancellationToken)
    {
        (IReadOnlyCollection<DomainPerson> persons, int totalCount) = await personReadRepository
           .GetPageAsync(
                pagination: request.Pagination,
                cancellationToken: cancellationToken);

        IReadOnlyCollection<PersonDto> dtos = persons.ToDtoCollection(timeProvider);

        return new PagedResult<PersonDto>(
            items: dtos,
            totalCount: totalCount,
            pageNumber: request.Pagination.PageNumber,
            pageSize: request.Pagination.PageSize);
    }
}
