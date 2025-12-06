using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.GetCitizenPage
{
    public sealed record GetCitizensPageQuery(Pagination Pagination)
        : IRequest<PagedResult<PersonDto>>;
}
