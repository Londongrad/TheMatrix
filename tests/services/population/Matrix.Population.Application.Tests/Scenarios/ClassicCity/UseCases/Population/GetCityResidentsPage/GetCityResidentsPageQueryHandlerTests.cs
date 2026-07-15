using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage
{
    public sealed class GetCityResidentsPageQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedPageAndPreservesRequestedCityAndPagination()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                PageByCityResult =
                (
                    Items:
                    [
                        CreatePerson(
                            personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
                            firstName: "Neo",
                            lastName: "Anderson",
                            birthDate: new DateOnly(
                                year: 2030,
                                month: 5,
                                day: 4),
                            currentDate: new DateOnly(
                                year: 2048,
                                month: 5,
                                day: 4)),
                        CreatePerson(
                            personId: Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"),
                            firstName: "Trinity",
                            lastName: "Moss",
                            birthDate: new DateOnly(
                                year: 2029,
                                month: 5,
                                day: 4),
                            currentDate: new DateOnly(
                                year: 2048,
                                month: 5,
                                day: 4))
                    ],
                    TotalCount: 18
                )
            };
            var pagination = new Pagination(
                pageNumber: 3,
                pageSize: 2);
            var educationParticipationRepository =
                new FakeEducationParticipationProjectionRepository();
            var firstResident = personReadRepository.PageByCityResult.Items.First();
            await educationParticipationRepository.UpsertNewerAsync(
            [
                new EducationParticipationProjection(
                    SimulationHostId: cityId,
                    ResidentId: firstResident.Id.Value,
                    ParticipationRevision: 2,
                    ResidentLifecycleRevision: firstResident.LifecycleRevision,
                    IsEnrolled: false,
                    ActiveStage: null,
                    InstitutionId: null,
                    InstitutionAnchorId: null,
                    EnrolledOn: null,
                    CompletedStage: "higher",
                    CompletedStageOn: new DateOnly(2048, 5, 3),
                    SnapshotDate: new DateOnly(2048, 5, 4),
                    OccurredAtUtc: UtcNow,
                    UpdatedAtUtc: UtcNow)
            ]);
            var handler = new GetCityResidentsPageQueryHandler(
                personReadRepository,
                educationParticipationRepository);

            PagedResult<PersonDto> result = await handler.Handle(
                request: new GetCityResidentsPageQuery(
                    CityId: cityId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 4),
                    Pagination: pagination),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            Assert.Equal(
                expected: pagination,
                actual: personReadRepository.RequestedPagination);
            Assert.Equal(
                expected: 18,
                actual: result.TotalCount);
            Assert.Equal(
                expected: 3,
                actual: result.PageNumber);
            Assert.Equal(
                expected: 2,
                actual: result.PageSize);
            PersonDto first = Assert.IsType<PersonDto>(result.Items.First());
            Assert.Equal(
                expected: "Anderson Neo",
                actual: first.FullName);
            Assert.Equal(
                expected: 18,
                actual: first.Age);
            Assert.Equal(
                expected: "Alive",
                actual: first.LifeStatus);
            Assert.Equal("higher", first.EducationLevel);
            Assert.Equal("none", result.Items.Last().EducationLevel);
            Assert.Equal(1, educationParticipationRepository.GetByResidentIdsCallCount);
            Assert.Equal(cityId, educationParticipationRepository.RequestedSimulationHostId);
            Assert.Equal(
                expected: personReadRepository.PageByCityResult.Items
                   .Select(person => person.Id.Value)
                   .OrderBy(id => id),
                actual: educationParticipationRepository.RequestedResidentIds.OrderBy(id => id));
        }

        [Fact]
        public async Task Handle_WhenPageIsEmpty_DoesNotQueryEducationProjection()
        {
            var personReadRepository = new FakeCityPopulationPersonReadRepository();
            var educationParticipationRepository =
                new FakeEducationParticipationProjectionRepository();
            var handler = new GetCityResidentsPageQueryHandler(
                personReadRepository,
                educationParticipationRepository);

            PagedResult<PersonDto> result = await handler.Handle(
                new GetCityResidentsPageQuery(
                    CityId: Guid.NewGuid(),
                    CurrentDate: new DateOnly(2048, 5, 4),
                    Pagination: new Pagination(pageNumber: 1, pageSize: 25)),
                CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, educationParticipationRepository.GetByResidentIdsCallCount);
        }
    }
}
