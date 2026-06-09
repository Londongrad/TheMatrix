using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
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

        [HttpGet("cities/{cityId:guid}/employment/catalog")]
        public async Task<ActionResult<CityEmploymentCatalogDto>> GetCityEmploymentCatalog(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken = default)
        {
            CityEmploymentCatalogDto result = await _sender.Send(
                request: new GetCityEmploymentCatalogQuery(cityId),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("cities/{cityId:guid}/education/catalog")]
        public async Task<ActionResult<CityEducationCatalogDto>> GetCityEducationCatalog(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken = default)
        {
            CityEducationCatalogDto result = await _sender.Send(
                request: new GetCityEducationCatalogQuery(cityId),
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/employment/hire")]
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

        [HttpPost("cities/{cityId:guid}/employment/fire")]
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

        [HttpPost("cities/{cityId:guid}/employment/retire")]
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

        [HttpPost("cities/{cityId:guid}/education/enroll")]
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

        [HttpPost("cities/{cityId:guid}/education/graduate")]
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

        [HttpPost("cities/{cityId:guid}/education/withdraw")]
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
