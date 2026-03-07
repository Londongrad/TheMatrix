using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog
{
    public sealed class GetCityEmploymentCatalogQueryHandler(
        IPopulationGenerationContentCatalog contentCatalog)
        : IRequestHandler<GetCityEmploymentCatalogQuery, CityEmploymentCatalogDto>
    {
        public Task<CityEmploymentCatalogDto> Handle(
            GetCityEmploymentCatalogQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> titles = contentCatalog.Professions
               .Select(x => x.Title)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
               .ToArray();

            return Task.FromResult(new CityEmploymentCatalogDto(titles));
        }
    }
}
