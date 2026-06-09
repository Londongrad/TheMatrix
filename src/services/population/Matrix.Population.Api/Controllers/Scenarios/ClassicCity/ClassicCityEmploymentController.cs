using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity;

[ApiController]
[Authorize]
[Route(ClassicCityPopulationApiRoutes.EmploymentRoute)]
public sealed class ClassicCityEmploymentController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("catalog")]
    public async Task<ActionResult<CityEmploymentCatalogDto>> GetCityEmploymentCatalog(
        [FromRoute] Guid cityId,
        CancellationToken cancellationToken = default)
    {
        CityEmploymentCatalogDto result = await _sender.Send(
            request: new GetCityEmploymentCatalogQuery(cityId),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("hire")]
    public async Task<ActionResult<CityEmploymentOperationResultDto>> HireResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEmploymentOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEmploymentOperationResultDto result = await _sender.Send(
            request: new HireCityResidentCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                JobTitle: request.JobTitle ?? string.Empty,
                WorkplaceId: request.WorkplaceId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("fire")]
    public async Task<ActionResult<CityEmploymentOperationResultDto>> FireResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEmploymentOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEmploymentOperationResultDto result = await _sender.Send(
            request: new FireCityResidentCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpPost("retire")]
    public async Task<ActionResult<CityEmploymentOperationResultDto>> RetireResident(
        [FromRoute] Guid cityId,
        [FromBody] CityEmploymentOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CityEmploymentOperationResultDto result = await _sender.Send(
            request: new RetireCityResidentCommand(
                CityId: cityId,
                ResidentId: request.ResidentId,
                CurrentDate: request.CurrentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
