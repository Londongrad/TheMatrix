using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.DTOs;
using MediatR;

namespace Matrix.Population.Application.UseCases.GetCitizenPage
{
    public sealed record GetCitizensPageQuery(Pagination Pagination)
    : IRequest<PagedResult<PersonDto>>;
}
