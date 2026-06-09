using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity;

[ApiController]
[Authorize]
[Route(ClassicCityPopulationApiRoutes.EducationRoute)]
public sealed class ClassicCityEducationController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("catalog")]
    public async Task<ActionResult<CityEducationCatalogDto>> GetCityEducationCatalog(
        [FromRoute] Guid cityId,
        CancellationToken cancellationToken = default)
    {
        CityEducationCatalogDto result = await _sender.Send(
            request: new GetCityEducationCatalogQuery(cityId),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("enroll")]
    public async Task<ActionResult<CityEducationOperationResultDto>> EnrollResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEducationOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEducationOperationResultDto result = await _sender.Send(
            request: new EnrollCityResidentCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                InstitutionId: request.InstitutionId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("graduate")]
    public async Task<ActionResult<CityEducationOperationResultDto>> GraduateResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEducationOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEducationOperationResultDto result = await _sender.Send(
            request: new GraduateCityResidentCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                TargetEducationLevel: request.TargetEducationLevel ?? string.Empty,
                InstitutionId: request.InstitutionId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<CityEducationOperationResultDto>> WithdrawResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEducationOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEducationOperationResultDto result = await _sender.Send(
            request: new WithdrawCityResidentFromStudyCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
