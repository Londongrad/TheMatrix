using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog
{
    public sealed record GetGenerationCatalogQuery : IRequest<CityGenerationCatalogDto>;
}
