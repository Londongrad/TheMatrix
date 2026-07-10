using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
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
