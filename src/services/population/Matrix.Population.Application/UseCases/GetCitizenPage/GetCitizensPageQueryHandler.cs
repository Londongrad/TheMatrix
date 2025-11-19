using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.GetCitizenPage
{
    public sealed class GetCitizensPageQueryHandler(
        IPersonReadRepository personReadRepository)
        : IRequestHandler<GetCitizensPageQuery, PagedResult<PersonDto>>
    {
        private readonly IPersonReadRepository _personReadRepository = personReadRepository;

        public async Task<PagedResult<PersonDto>> Handle(
            GetCitizensPageQuery request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var (persons, totalCount) = await _personReadRepository
                .GetPageAsync(request.Pagination, cancellationToken);

            var dtos = persons.ToDtoCollection();

            return new PagedResult<PersonDto>
            (
                items: dtos,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize
            );
        }
    }
}
