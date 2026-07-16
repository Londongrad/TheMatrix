using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Mapping;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository)
        : IRequestHandler<GetCityResidentsPageQuery, PagedResult<CityResidentSummaryDto>>
    {
        public async Task<PagedResult<CityResidentSummaryDto>> Handle(
            GetCityResidentsPageQuery request,
            CancellationToken cancellationToken)
        {
            (IReadOnlyCollection<Person> persons, int totalCount) = await personReadRepository.GetPageByCityAsync(
                cityId: CityId.From(request.CityId),
                pagination: request.Pagination,
                cancellationToken: cancellationToken);

            IReadOnlyDictionary<Guid, EducationParticipationProjection> educationParticipations =
                persons.Count == 0
                    ? new Dictionary<Guid, EducationParticipationProjection>()
                    : await educationParticipationProjectionRepository.GetByResidentIdsAsync(
                        simulationHostId: request.CityId,
                        residentIds: persons.Select(person => person.Id.Value).ToArray(),
                        cancellationToken: cancellationToken);
            var educationParticipationIndex = new EducationParticipationProjectionIndex(
                request.CityId,
                educationParticipations);
            IReadOnlyCollection<CityResidentSummaryDto> dtos = persons
               .Select(person => person.ToResidentSummaryDto(
                    currentDate: request.CurrentDate,
                    attainedEducationStage: ResolveAttainedEducationStage(
                        person,
                        educationParticipationIndex)))
               .ToArray();

            return new PagedResult<CityResidentSummaryDto>(
                items: dtos,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }

        private static string ResolveAttainedEducationStage(
            Person person,
            EducationParticipationProjectionIndex educationParticipationIndex)
        {
            return educationParticipationIndex.FindCurrent(person)?.CompletedStage ?? "none";
        }
    }
}
