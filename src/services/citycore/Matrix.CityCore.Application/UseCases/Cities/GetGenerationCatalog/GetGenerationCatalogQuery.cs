using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.GetGenerationCatalog
{
    public sealed record GetGenerationCatalogQuery : IRequest<CityGenerationCatalogDto>;
}
