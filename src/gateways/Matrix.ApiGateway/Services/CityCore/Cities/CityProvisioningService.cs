using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Matrix.ApiGateway.Contracts.CityCore.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.CityCore.Contracts.Cities;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using Matrix.CityCore.Contracts.Simulation.Views;
using Matrix.CityCore.Contracts.Topology.Views;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.Services.CityCore.Cities
{
    public sealed class CityProvisioningService(
        ICitiesApiClient citiesApiClient,
        ISimulationApiClient simulationApiClient,
        IPopulationApiClient populationApiClient,
        ILogger<CityProvisioningService> logger) : ICityProvisioningService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<CityProvisioningView> CreateCityAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityCreatedView created = await citiesApiClient.CreateCityAsync(
                request: new CreateCityRequest(
                    Name: request.Name,
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes,
                    GenerationSeed: request.GenerationSeed,
                    SizeTier: request.SizeTier,
                    UrbanDensity: request.UrbanDensity,
                    DevelopmentLevel: request.DevelopmentLevel,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier),
                cancellationToken: cancellationToken);

            CityPopulationBootstrapView bootstrap = await BootstrapPopulationAsync(
                cityId: created.CityId,
                operationId: created.PopulationBootstrapOperationId,
                cancellationToken: cancellationToken);

            await ReportBootstrapOutcomeAsync(
                cityId: created.CityId,
                bootstrap: bootstrap,
                cancellationToken: cancellationToken);

            return new CityProvisioningView(
                CityId: created.CityId,
                PopulationBootstrap: bootstrap);
        }

        private async Task<CityPopulationBootstrapView> BootstrapPopulationAsync(
            Guid cityId,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            int? plannedPeopleCount = null;
            int? residentialCapacity = null;

            try
            {
                Task<CityView> cityTask = citiesApiClient.GetCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
                Task<SimulationClockView> clockTask = simulationApiClient.GetClockAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
                Task<IReadOnlyList<ResidentialBuildingView>> buildingsTask =
                    citiesApiClient.GetResidentialBuildingsAsync(
                        cityId: cityId,
                        districtId: null,
                        cancellationToken: cancellationToken);

                await Task.WhenAll(
                    cityTask,
                    clockTask,
                    buildingsTask);

                CityView city = await cityTask;
                SimulationClockView clock = await clockTask;
                IReadOnlyList<ResidentialBuildingView> buildings = await buildingsTask;

                residentialCapacity = buildings.Sum(x => x.ResidentCapacity);
                plannedPeopleCount = CalculatePeopleCount(
                    city: city,
                    buildings: buildings);

                var populationRequest = new InitializeCityPopulationRequest(
                    CityId: cityId,
                    CurrentDate: DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime),
                    PeopleCount: plannedPeopleCount.Value,
                    RandomSeed: BuildPopulationRandomSeed(city.GenerationSeed),
                    ResidentialBuildings: buildings
                       .Select(x => new ResidentialBuildingSeedDto(
                            ResidentialBuildingId: x.ResidentialBuildingId,
                            DistrictId: x.DistrictId,
                            ResidentCapacity: x.ResidentCapacity))
                       .ToArray());

                CityPopulationBootstrapSummaryDto summary =
                    await populationApiClient.InitializeCityPopulationAsync(
                        request: populationRequest,
                        cancellationToken: cancellationToken);

                return new CityPopulationBootstrapView(
                    OperationId: operationId,
                    Status: PopulationBootstrapStatuses.Completed,
                    PlannedPeopleCount: plannedPeopleCount,
                    ResidentialCapacity: residentialCapacity,
                    Summary: summary,
                    FailureCode: null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Automatic population bootstrap timed out for cityId={CityId}.",
                    cityId);

                return new CityPopulationBootstrapView(
                    OperationId: operationId,
                    Status: PopulationBootstrapStatuses.Failed,
                    PlannedPeopleCount: plannedPeopleCount,
                    ResidentialCapacity: residentialCapacity,
                    Summary: null,
                    FailureCode: PopulationBootstrapFailureCodes.PopulationTimeout);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Automatic population bootstrap failed for cityId={CityId}.",
                    cityId);

                return new CityPopulationBootstrapView(
                    OperationId: operationId,
                    Status: PopulationBootstrapStatuses.Failed,
                    PlannedPeopleCount: plannedPeopleCount,
                    ResidentialCapacity: residentialCapacity,
                    Summary: null,
                    FailureCode: DetermineFailureCode(ex));
            }
        }

        private async Task ReportBootstrapOutcomeAsync(
            Guid cityId,
            CityPopulationBootstrapView bootstrap,
            CancellationToken cancellationToken)
        {
            switch (bootstrap.Status)
            {
                case PopulationBootstrapStatuses.Completed:
                    await citiesApiClient.CompletePopulationBootstrapAsync(
                        cityId: cityId,
                        request: new CompleteCityPopulationBootstrapRequest(OperationId: bootstrap.OperationId),
                        cancellationToken: cancellationToken);
                    break;

                case PopulationBootstrapStatuses.Failed:
                    await citiesApiClient.FailPopulationBootstrapAsync(
                        cityId: cityId,
                        request: new FailCityPopulationBootstrapRequest(
                            OperationId: bootstrap.OperationId,
                            FailureCode: bootstrap.FailureCode ??
                                         PopulationBootstrapFailureCodes.PopulationUnexpectedError),
                        cancellationToken: cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported population bootstrap status '{bootstrap.Status}'.");
            }
        }

        private static int CalculatePeopleCount(
            CityView city,
            IReadOnlyCollection<ResidentialBuildingView> buildings)
        {
            int totalCapacity = buildings.Sum(x => x.ResidentCapacity);
            if (totalCapacity <= 0)
                return 0;

            // Keep the bootstrap population comfortably below hard capacity.
            decimal occupancyRate = GetBaseOccupancy(city.UrbanDensity) +
                                    GetDevelopmentAdjustment(city.DevelopmentLevel) +
                                    GetSizeAdjustment(city.SizeTier) +
                                    GetSeedJitter(city.GenerationSeed);

            occupancyRate = Math.Clamp(
                value: occupancyRate,
                min: 0.35m,
                max: 0.95m);

            int minimumPopulation = Math.Min(
                val1: totalCapacity,
                val2: Math.Max(
                    val1: 1,
                    val2: buildings.Count));

            int plannedPeopleCount = (int)Math.Round(
                d: totalCapacity * occupancyRate,
                mode: MidpointRounding.AwayFromZero);

            return Math.Clamp(
                value: plannedPeopleCount,
                min: minimumPopulation,
                max: totalCapacity);
        }

        private static decimal GetBaseOccupancy(string urbanDensity)
        {
            return urbanDensity switch
            {
                "Sparse" => 0.48m,
                "Dense" => 0.84m,
                _ => 0.66m
            };
        }

        private static decimal GetDevelopmentAdjustment(string developmentLevel)
        {
            return developmentLevel switch
            {
                "Struggling" => -0.08m,
                "Advanced" => 0.05m,
                _ => 0.0m
            };
        }

        private static decimal GetSizeAdjustment(string sizeTier)
        {
            return sizeTier switch
            {
                "Small" => -0.03m,
                "Large" => 0.04m,
                _ => 0.0m
            };
        }

        private static decimal GetSeedJitter(string generationSeed)
        {
            byte[] hash = SHA256.HashData(source: Encoding.UTF8.GetBytes($"{generationSeed}|population-bootstrap"));
            int sample = BitConverter.ToInt32(
                             value: hash,
                             startIndex: 0) &
                         int.MaxValue;

            decimal normalized = sample / (decimal)int.MaxValue;
            return (normalized - 0.5m) * 0.08m;
        }

        private static int BuildPopulationRandomSeed(string generationSeed)
        {
            byte[] hash = SHA256.HashData(source: Encoding.UTF8.GetBytes($"{generationSeed}|population-seed"));

            return BitConverter.ToInt32(
                value: hash,
                startIndex: 0);
        }

        private static string DetermineFailureCode(Exception exception)
        {
            return exception switch
            {
                DownstreamServiceException downstreamException when
                    TryReadDownstreamErrorCode(
                        exception: downstreamException,
                        errorCode: out string? errorCode) &&
                    errorCode is "Gateway.InvalidDownstreamResponse" or "Gateway.InvalidDownstreamJson" =>
                    PopulationBootstrapFailureCodes.PopulationResponseInvalid,
                DownstreamServiceException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                    PopulationBootstrapFailureCodes.PopulationValidationFailed,
                DownstreamServiceException downstreamException when downstreamException.StatusCode ==
                                                                    HttpStatusCode.Conflict =>
                    PopulationBootstrapFailureCodes.PopulationConflict,
                DownstreamServiceException downstreamException when downstreamException.StatusCode ==
                                                                    HttpStatusCode.NotFound =>
                    PopulationBootstrapFailureCodes.PopulationDependencyNotFound,
                DownstreamServiceException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                    PopulationBootstrapFailureCodes.PopulationTimeout,
                DownstreamServiceException downstreamException when (int)downstreamException.StatusCode >= 500 =>
                    PopulationBootstrapFailureCodes.PopulationServiceUnavailable,
                HttpRequestException => PopulationBootstrapFailureCodes.PopulationTransportError,
                OperationCanceledException => PopulationBootstrapFailureCodes.PopulationTimeout,
                _ => PopulationBootstrapFailureCodes.PopulationUnexpectedError
            };
        }

        private static bool TryReadDownstreamErrorCode(
            DownstreamServiceException exception,
            out string? errorCode)
        {
            errorCode = null;

            if (string.IsNullOrWhiteSpace(exception.Body))
                return false;

            try
            {
                ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(
                    json: exception.Body,
                    options: JsonOptions);

                if (error is null || string.IsNullOrWhiteSpace(error.Code))
                    return false;

                errorCode = error.Code;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static class PopulationBootstrapStatuses
        {
            public const string Completed = "Completed";
            public const string Failed = "Failed";
        }
    }
}
