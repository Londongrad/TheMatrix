using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route(ClassicCityPopulationApiRoutes.PopulationRoute)]
    public class PopulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPost("cities/{cityId:guid}/civil-registry/marriages")]
        public async Task<ActionResult<CityCivilRegistryOperationResultDto>> RegisterMarriage(
            [FromRoute] Guid cityId,
            [FromBody] CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityCivilRegistryOperationResultDto result = await _sender.Send(
                request: new RegisterCityMarriageCommand(
                    CityId: cityId,
                    FirstResidentId: request.FirstResidentId,
                    SecondResidentId: request.SecondResidentId,
                    CurrentDate: request.CurrentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/civil-registry/divorces")]
        public async Task<ActionResult<CityCivilRegistryOperationResultDto>> RegisterDivorce(
            [FromRoute] Guid cityId,
            [FromBody] CityCivilRegistryOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityCivilRegistryOperationResultDto result = await _sender.Send(
                request: new RegisterCityDivorceCommand(
                    CityId: cityId,
                    FirstResidentId: request.FirstResidentId,
                    SecondResidentId: request.SecondResidentId,
                    CurrentDate: request.CurrentDate),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

    }
}
