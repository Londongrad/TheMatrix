using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using MediatR;

namespace Matrix.Population.Application.UseCases.GetCitizenPage
{
    public sealed class GetCitizensPageQueryHandler(IPersonReadRepository personReadRepository)
        : IRequestHandler<GetCitizensPageQuery, PagedResult<PersonDto>>
    {
        public async Task<PagedResult<PersonDto>> Handle(
            GetCitizensPageQuery request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(argument: request);

            (IReadOnlyCollection<Person> persons, int totalCount) = await personReadRepository
               .GetPageAsync(
                    pagination: request.Pagination,
                    cancellationToken: cancellationToken);

            IReadOnlyCollection<PersonDto> dtos = persons.ToDtoCollection();

            return new PagedResult<PersonDto>(
                items: dtos,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }
    }
}
