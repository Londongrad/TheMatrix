using MediatR;

namespace Matrix.Education.Application.Institutions.ListEducationInstitutions;

public sealed record ListEducationInstitutionsQuery(Guid SimulationHostId)
    : IRequest<IReadOnlyList<EducationInstitutionView>>;
