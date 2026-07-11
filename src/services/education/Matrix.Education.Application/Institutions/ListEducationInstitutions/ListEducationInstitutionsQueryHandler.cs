using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using MediatR;

namespace Matrix.Education.Application.Institutions.ListEducationInstitutions;

public sealed class ListEducationInstitutionsQueryHandler(
    IEducationInstitutionRepository institutionRepository)
    : IRequestHandler<ListEducationInstitutionsQuery, IReadOnlyList<EducationInstitutionView>>
{
    public async Task<IReadOnlyList<EducationInstitutionView>> Handle(
        ListEducationInstitutionsQuery request,
        CancellationToken cancellationToken)
    {
        var simulationHostId = new SimulationHostId(request.SimulationHostId);
        IReadOnlyList<EducationInstitution> institutions =
            await institutionRepository.ListActiveAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

        return institutions
           .Select(institution => new EducationInstitutionView(
                InstitutionId: institution.EducationInstitutionId.Value,
                Name: institution.Name,
                Kind: institution.Kind.Value,
                LocationAnchorId: institution.LocationAnchorId?.Value,
                Capacity: institution.Capacity,
                CurrentEnrollmentCount: institution.CurrentEnrollmentCount,
                AvailableSeatCount: institution.AvailableSeatCount))
           .ToArray();
    }
}
