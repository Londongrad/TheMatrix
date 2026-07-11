using Matrix.Education.Application.Institutions.ListEducationInstitutions;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Application.Tests.Institutions.ListEducationInstitutions;

public sealed class ListEducationInstitutionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsActiveInstitutionsAndCapacityState()
    {
        var simulationHostId = new SimulationHostId(
            Guid.Parse("11111111-2222-3333-4444-555555555555"));
        var institutionId = new EducationInstitutionId(
            Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"));
        var locationAnchorId = new LocationAnchorId(
            Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"));
        EducationInstitution institution = EducationInstitution.Create(
            id: institutionId,
            simulationHostId: simulationHostId,
            name: "Central Education Complex",
            kind: new EducationInstitutionKindKey("School"),
            capacity: 640,
            locationAnchorId: locationAnchorId);
        Assert.True(institution.TryReserveSeats(17));
        var repository = new EducationInstitutionRepositoryStub(institution);
        var handler = new ListEducationInstitutionsQueryHandler(repository);

        IReadOnlyList<EducationInstitutionView> result = await handler.Handle(
            request: new ListEducationInstitutionsQuery(simulationHostId.Value),
            cancellationToken: CancellationToken.None);

        Assert.Equal(
            expected: 1,
            actual: repository.ListActiveCallCount);
        EducationInstitutionView view = Assert.Single(result);
        Assert.Equal(
            expected: institutionId.Value,
            actual: view.InstitutionId);
        Assert.Equal(
            expected: "Central Education Complex",
            actual: view.Name);
        Assert.Equal(
            expected: "school",
            actual: view.Kind);
        Assert.Equal(
            expected: locationAnchorId.Value,
            actual: view.LocationAnchorId);
        Assert.Equal(
            expected: 640,
            actual: view.Capacity);
        Assert.Equal(
            expected: 17,
            actual: view.CurrentEnrollmentCount);
        Assert.Equal(
            expected: 623,
            actual: view.AvailableSeatCount);
    }
}
