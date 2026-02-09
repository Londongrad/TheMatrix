using Matrix.CityCore.Application.Services.Generation.Abstractions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog
{
    public sealed class GetGenerationCatalogQueryHandler(ICityGenerationContentCatalog contentCatalog)
        : IRequestHandler<GetGenerationCatalogQuery, CityGenerationCatalogDto>
    {
        public Task<CityGenerationCatalogDto> Handle(
            GetGenerationCatalogQuery request,
            CancellationToken cancellationToken)
        {
            var result = new CityGenerationCatalogDto(
                CityNamePresets: contentCatalog.CityNamePresets.ToArray(),
                DistrictNamePresets: contentCatalog.DistrictNamePresets.ToArray(),
                StreetNamePresets: contentCatalog.StreetNamePresets.ToArray());

            return Task.FromResult(result);
        }
    }
}
