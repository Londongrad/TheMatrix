using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Mapping;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        IEducationParticipationProjectionRepository educationParticipationProjectionRepository)
        : IRequestHandler<GetCityResidentsPageQuery, PagedResult<PersonDto>>
    {
        public async Task<PagedResult<PersonDto>> Handle(
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
            IReadOnlyCollection<PersonDto> dtos = persons
               .Select(person => person.ToDto(
                    currentDate: request.CurrentDate,
                    attainedEducationStage: ResolveAttainedEducationStage(
                        person,
                        educationParticipations)))
               .ToArray();

            return new PagedResult<PersonDto>(
                items: dtos,
                totalCount: totalCount,
                pageNumber: request.Pagination.PageNumber,
                pageSize: request.Pagination.PageSize);
        }

        private static string ResolveAttainedEducationStage(
            Person person,
            IReadOnlyDictionary<Guid, EducationParticipationProjection> educationParticipations)
        {
            return educationParticipations.TryGetValue(
                       person.Id.Value,
                       out EducationParticipationProjection? projection)
                   && projection.ResidentLifecycleRevision == person.LifecycleRevision
                ? projection.CompletedStage ?? "none"
                : "none";
        }
    }
}
