using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Application.Institutions.ListEducationInstitutions;
using Matrix.Education.Contracts;
using Matrix.Education.Contracts.Institutions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Education.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route(EducationApiRoutes.Institutions)]
    public sealed class EducationInstitutionsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<EducationInstitutionCatalogResponse>> List(
            [FromRoute] Guid simulationHostId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EducationInstitutionView> institutions = await sender.Send(
                new ListEducationInstitutionsQuery(simulationHostId),
                cancellationToken);

            return Ok(new EducationInstitutionCatalogResponse(
                Institutions: institutions
                   .Select(institution => new EducationInstitutionResponse(
                        InstitutionId: institution.InstitutionId,
                        Name: institution.Name,
                        Kind: institution.Kind,
                        LocationAnchorId: institution.LocationAnchorId,
                        Capacity: institution.Capacity,
                        CurrentEnrollmentCount: institution.CurrentEnrollmentCount,
                        AvailableSeatCount: institution.AvailableSeatCount))
                   .ToArray()));
        }

        [HttpPut]
        public async Task<ActionResult<SynchronizeEducationInstitutionsResponse>> Synchronize(
            [FromRoute] Guid simulationHostId,
            [FromBody] SynchronizeEducationInstitutionsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            SynchronizeEducationInstitutionsResult result = await sender.Send(
                new SynchronizeEducationInstitutionsCommand(
                    SimulationHostId: simulationHostId,
                    SourceRevision: request.SourceRevision,
                    SynchronizedAtUtc: request.SynchronizedAtUtc,
                    Institutions: request.Institutions.Select(item =>
                            new SynchronizeEducationInstitutionItem(
                                InstitutionId: item.InstitutionId,
                                Name: item.Name,
                                Kind: item.Kind,
                                Capacity: item.Capacity,
                                IsActive: item.IsActive,
                                LocationAnchorId: item.LocationAnchorId))
                       .ToArray()),
                cancellationToken);

            return Ok(new SynchronizeEducationInstitutionsResponse(
                Status: result.Status.ToString(),
                AddedInstitutions: result.AddedInstitutions,
                UpdatedInstitutions: result.UpdatedInstitutions,
                IgnoredInstitutions: result.IgnoredInstitutions));
        }
    }
}
