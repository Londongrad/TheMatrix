using AutoMapper;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.DTOs;
using MediatR;

namespace Matrix.Population.Application.UseCases.GetCitizenPage
{
    public sealed class GetCitizensPageQueryHandler(
        IPersonReadRepository personReadRepository,
        IMapper mapper)
        : IRequestHandler<GetCitizensPageQuery, PagedResult<PersonDto>>
    {
        private readonly IPersonReadRepository _personReadRepository = personReadRepository;

        public async Task<PagedResult<PersonDto>> Handle(
            GetCitizensPageQuery request,
            CancellationToken cancellationToken)
        {
            var (persons, totalCount) = await _personReadRepository
                .GetPageAsync(request.Pagination, cancellationToken);

            var dtos = mapper.Map<IReadOnlyCollection<PersonDto>>(persons);

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
