using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy.Models;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed class CityProvisioningService(
        ICitiesApiClient citiesApiClient,
        ISimulationApiClient simulationApiClient,
        IEconomyApiClient economyApiClient,
        IPopulationApiClient populationApiClient,
        ILogger<CityProvisioningService> logger) : ICityProvisioningService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<CityProvisioningView> CreateCityAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityCreatedView created = await CreateCitySkeletonAsync(
                request: request,
                cancellationToken: cancellationToken);

            return await ProvisionCreatedCityAsync(
                cityId: created.CityId,
                simulationKind: created.SimulationKind,
                populationOperationId: created.PopulationBootstrapOperationId,
                economyOperationId: created.EconomyBootstrapOperationId,
                cancellationToken: cancellationToken);
        }

        public async Task<CityCreatedView> CreateCitySkeletonAsync(
            CreateCityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return await citiesApiClient.CreateCityAsync(
                request: new CreateCityRequest(
                    Name: request.Name,
                    SimulationKind: request.SimulationKind,
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes,
                    GenerationSeed: request.GenerationSeed,
                    SizeTier: request.SizeTier,
                    UrbanDensity: request.UrbanDensity,
                    DevelopmentLevel: request.DevelopmentLevel,
                    EconomyProfile: request.EconomyProfile,
                    PopulationOccupancyProfile: request.PopulationOccupancyProfile,
                    InitialWeatherMode: request.InitialWeatherMode,
                    InitialWeatherType: request.InitialWeatherType,
                    InitialWeatherSeverity: request.InitialWeatherSeverity,
                    InitialWeatherTemperatureC: request.InitialWeatherTemperatureC,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier,
                    PlannedPeopleCount: request.PlannedPeopleCount,
                    ProvisioningCorrelationId: request.ProvisioningCorrelationId),
                cancellationToken: cancellationToken);
        }

        public async Task<CityProvisioningView> RetryPopulationBootstrapAsync(
            Guid cityId,
            int? plannedPeopleCountOverride = null,
            CancellationToken cancellationToken = default)
        {
            CityPopulationBootstrapRestartedView restarted = await citiesApiClient.RestartPopulationBootstrapAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return await ProvisionCreatedCityAsync(
                cityId: restarted.CityId,
                simulationKind: restarted.SimulationKind,
                populationOperationId: restarted.PopulationBootstrapOperationId,
                economyOperationId: restarted.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: plannedPeopleCountOverride,
                cancellationToken: cancellationToken);
        }

        public async Task<CityProvisioningView> ProvisionCreatedCityAsync(
            Guid cityId,
            string simulationKind,
            Guid populationOperationId,
            Guid economyOperationId,
            CancellationToken cancellationToken = default,
            int? plannedPeopleCountOverride = null)
        {
            CityEconomyBootstrapView economyBootstrap = await BootstrapEconomyAsync(
                cityId: cityId,
                simulationKind: simulationKind,
                operationId: economyOperationId,
                cancellationToken: cancellationToken);

            await ReportEconomyBootstrapOutcomeAsync(
                cityId: cityId,
                bootstrap: economyBootstrap,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    a: economyBootstrap.Status,
                    b: EconomyBootstrapStatuses.Completed,
                    comparisonType: StringComparison.Ordinal))
                return new CityProvisioningView(
                    CityId: cityId,
                    SimulationKind: simulationKind,
                    PopulationBootstrap: new CityPopulationBootstrapView(
                        OperationId: populationOperationId,
                        Status: PopulationBootstrapStatuses.Pending,
                        PlannedPeopleCount: plannedPeopleCountOverride,
                        ResidentialCapacity: null,
                        Summary: null,
                        FailureCode: null),
                    EconomyBootstrap: economyBootstrap);

            bool supportsAutomaticPopulationBootstrap = await SupportsAutomaticPopulationBootstrapAsync(
                simulationKind: simulationKind,
                cancellationToken: cancellationToken);

            if (!supportsAutomaticPopulationBootstrap)
                return new CityProvisioningView(
                    CityId: cityId,
                    SimulationKind: simulationKind,
                    PopulationBootstrap: new CityPopulationBootstrapView(
                        OperationId: populationOperationId,
                        Status: PopulationBootstrapStatuses.Skipped,
                        PlannedPeopleCount: null,
                        ResidentialCapacity: null,
                        Summary: null,
                        FailureCode: null),
                    EconomyBootstrap: economyBootstrap);

            CityPopulationBootstrapView bootstrap = await BootstrapPopulationAsync(
                cityId: cityId,
                operationId: populationOperationId,
                plannedPeopleCountOverride: plannedPeopleCountOverride,
                cancellationToken: cancellationToken);

            await ReportBootstrapOutcomeAsync(
                cityId: cityId,
                bootstrap: bootstrap,
                cancellationToken: cancellationToken);

            return new CityProvisioningView(
                CityId: cityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: bootstrap,
                EconomyBootstrap: economyBootstrap);
        }

        private async Task<CityEconomyBootstrapView> BootstrapEconomyAsync(
            Guid cityId,
            string simulationKind,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            try
            {
                CityView city = await citiesApiClient.GetCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

                CityEconomyBootstrapResultDto result = await economyApiClient.InitializeCityEconomyAsync(
                    cityId: cityId,
                    request: new InitializeCityEconomyRequestDto(
                        SimulationKind: simulationKind,
                        EconomyProfile: city.EconomyProfile,
                        CreatedAtUtc: city.CreatedAtUtc),
                    cancellationToken: cancellationToken);

                return new CityEconomyBootstrapView(
                    OperationId: operationId,
                    Status: EconomyBootstrapStatuses.Completed,
                    FailureCode: null,
                    UnitKind: result.UnitKind,
                    UnitCode: result.UnitCode,
                    UnitDisplayName: result.UnitDisplayName,
                    UnitSymbol: result.UnitSymbol);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return BuildFailedEconomyBootstrap(
                    operationId: operationId,
                    failureCode: EconomyBootstrapFailureCodes.EconomyTimeout);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Automatic economy bootstrap failed for cityId={CityId}.",
                    cityId);

                return BuildFailedEconomyBootstrap(
                    operationId: operationId,
                    failureCode: DetermineEconomyFailureCode(ex));
            }
        }

        private async Task<CityPopulationBootstrapView> BootstrapPopulationAsync(
            Guid cityId,
            Guid operationId,
            CancellationToken cancellationToken,
            int? plannedPeopleCountOverride = null)
        {
            int? plannedPeopleCount = null;
            int? residentialCapacity = null;

            try
            {
                Task<CityView> cityTask = citiesApiClient.GetCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
                Task<SimulationClockView> clockTask = simulationApiClient.GetClockAsync(
                    simulationId: cityId,
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
                plannedPeopleCount = ResolvePlannedPeopleCount(
                    plannedPeopleCountOverride: plannedPeopleCountOverride,
                    city: city,
                    buildings: buildings);

                if (residentialCapacity <= 0)
                {
                    logger.LogWarning(
                        message:
                        "Automatic population bootstrap aborted for cityId={CityId} because generated topology has no residential capacity.",
                        cityId);

                    return new CityPopulationBootstrapView(
                        OperationId: operationId,
                        Status: PopulationBootstrapStatuses.Failed,
                        PlannedPeopleCount: plannedPeopleCount,
                        ResidentialCapacity: residentialCapacity,
                        Summary: null,
                        FailureCode: PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing);
                }

                var populationRequest = new InitializeCityPopulationRequest(
                    CityId: cityId,
                    CurrentDate: DateOnly.FromDateTime(clock.SimTimeUtc.UtcDateTime),
                    PeopleCount: plannedPeopleCount.Value,
                    RandomSeed: BuildPopulationRandomSeed(city.GenerationSeed),
                    Environment: new CityPopulationEnvironmentDto(
                        ClimateZone: city.ClimateZone,
                        Hemisphere: city.Hemisphere,
                        UtcOffsetMinutes: city.UtcOffsetMinutes),
                    Tuning: BuildBootstrapTuning(
                        city: city,
                        plannedPeopleCount: plannedPeopleCount.Value,
                        residentialCapacity: residentialCapacity.Value),
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

                if (!TryValidateBootstrapSummary(
                        cityId: cityId,
                        summary: summary,
                        plannedPeopleCount: plannedPeopleCount.Value,
                        residentialCapacity: residentialCapacity.Value,
                        failureReason: out string? failureReason))
                {
                    logger.LogWarning(
                        message:
                        "Automatic population bootstrap returned an inconsistent summary for cityId={CityId}. Reason: {FailureReason}",
                        cityId,
                        failureReason);

                    return new CityPopulationBootstrapView(
                        OperationId: operationId,
                        Status: PopulationBootstrapStatuses.Failed,
                        PlannedPeopleCount: plannedPeopleCount,
                        ResidentialCapacity: residentialCapacity,
                        Summary: null,
                        FailureCode: PopulationBootstrapFailureCodes.PopulationSummaryInconsistent);
                }

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

        private async Task ReportEconomyBootstrapOutcomeAsync(
            Guid cityId,
            CityEconomyBootstrapView bootstrap,
            CancellationToken cancellationToken)
        {
            switch (bootstrap.Status)
            {
                case EconomyBootstrapStatuses.Completed:
                    await citiesApiClient.CompleteEconomyBootstrapAsync(
                        cityId: cityId,
                        request: new CompleteCityEconomyBootstrapRequest(OperationId: bootstrap.OperationId),
                        cancellationToken: cancellationToken);
                    break;

                case EconomyBootstrapStatuses.Failed:
                    await citiesApiClient.FailEconomyBootstrapAsync(
                        cityId: cityId,
                        request: new FailCityEconomyBootstrapRequest(
                            OperationId: bootstrap.OperationId,
                            FailureCode: bootstrap.FailureCode ?? EconomyBootstrapFailureCodes.EconomyUnexpectedError),
                        cancellationToken: cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported economy bootstrap status '{bootstrap.Status}'.");
            }
        }

        private static int ResolvePlannedPeopleCount(
            int? plannedPeopleCountOverride,
            CityView city,
            IReadOnlyCollection<ResidentialBuildingView> buildings)
        {
            if (plannedPeopleCountOverride.HasValue)
                return Math.Max(
                    val1: 0,
                    val2: plannedPeopleCountOverride.Value);

            if (city.PlannedPeopleCount.HasValue)
                return Math.Max(
                    val1: 0,
                    val2: city.PlannedPeopleCount.Value);

            return CalculateAutomaticPeopleCount(
                city: city,
                buildings: buildings);
        }

        private static int CalculateAutomaticPeopleCount(
            CityView city,
            IReadOnlyCollection<ResidentialBuildingView> buildings)
        {
            int totalCapacity = buildings.Sum(x => x.ResidentCapacity);
            if (totalCapacity <= 0)
                return 0;

            // Keep the bootstrap population comfortably below hard capacity.
            decimal occupancyRate = GetBaseOccupancy(city.PopulationOccupancyProfile) +
                                    GetDensityAdjustment(city.UrbanDensity) +
                                    GetDevelopmentAdjustment(city.DevelopmentLevel) +
                                    GetSeedJitter(city.GenerationSeed);

            occupancyRate = Math.Clamp(
                value: occupancyRate,
                min: 0.30m,
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

        private static CityPopulationBootstrapTuningDto BuildBootstrapTuning(
            CityView city,
            int plannedPeopleCount,
            int residentialCapacity)
        {
            int housingPressurePercent = CalculateHousingPressurePercent(
                city: city,
                plannedPeopleCount: plannedPeopleCount,
                residentialCapacity: residentialCapacity);
            int economicStabilityPercent = CalculateEconomicStabilityPercent(
                city: city,
                housingPressurePercent: housingPressurePercent);
            int socialVolatilityPercent = CalculateSocialVolatilityPercent(
                city: city,
                housingPressurePercent: housingPressurePercent);
            int familyFormationPercent = CalculateFamilyFormationPercent(
                city: city,
                housingPressurePercent: housingPressurePercent);

            return new CityPopulationBootstrapTuningDto(
                HousingPressurePercent: housingPressurePercent,
                EconomicStabilityPercent: economicStabilityPercent,
                SocialVolatilityPercent: socialVolatilityPercent,
                FamilyFormationPercent: familyFormationPercent);
        }

        private static int CalculateHousingPressurePercent(
            CityView city,
            int plannedPeopleCount,
            int residentialCapacity)
        {
            decimal occupancyRatio = residentialCapacity <= 0
                ? 1.20m
                : plannedPeopleCount / (decimal)residentialCapacity;

            int pressure = 45 + (int)Math.Round((occupancyRatio - 0.70m) * 110m);

            pressure += city.UrbanDensity switch
            {
                "Sparse" => -10,
                "Dense" => +12,
                _ => 0
            };

            pressure += city.DevelopmentLevel switch
            {
                "Struggling" => +10,
                "Advanced" => -8,
                _ => 0
            };

            pressure += city.PopulationOccupancyProfile switch
            {
                "Light" => -8,
                "High" => +10,
                _ => 0
            };

            pressure += city.SizeTier switch
            {
                "Small" => +4,
                "Large" => -3,
                _ => 0
            };

            pressure += GetSeedJitterPoints(
                generationSeed: city.GenerationSeed,
                salt: "population-housing-pressure",
                maxAbsPoints: 5);

            return Math.Clamp(
                value: pressure,
                min: 0,
                max: 100);
        }

        private static int CalculateEconomicStabilityPercent(
            CityView city,
            int housingPressurePercent)
        {
            int stability = 52;

            stability += city.DevelopmentLevel switch
            {
                "Struggling" => -18,
                "Advanced" => +16,
                _ => 0
            };

            stability += city.UrbanDensity switch
            {
                "Sparse" => -4,
                "Dense" => +4,
                _ => 0
            };

            stability += city.SizeTier switch
            {
                "Small" => -4,
                "Large" => +6,
                _ => 0
            };

            stability += city.PopulationOccupancyProfile switch
            {
                "Light" => +4,
                "High" => -6,
                _ => 0
            };

            stability -= (int)Math.Round(
                Math.Max(
                    val1: 0,
                    val2: housingPressurePercent - 50) *
                0.40m);
            stability += GetSeedJitterPoints(
                generationSeed: city.GenerationSeed,
                salt: "population-economic-stability",
                maxAbsPoints: 6);

            return Math.Clamp(
                value: stability,
                min: 0,
                max: 100);
        }

        private static int CalculateSocialVolatilityPercent(
            CityView city,
            int housingPressurePercent)
        {
            int volatility = 40;

            volatility += city.UrbanDensity switch
            {
                "Sparse" => -8,
                "Dense" => +14,
                _ => 0
            };

            volatility += city.DevelopmentLevel switch
            {
                "Struggling" => +12,
                "Advanced" => -6,
                _ => 0
            };

            volatility += city.PopulationOccupancyProfile switch
            {
                "Light" => -6,
                "High" => +10,
                _ => 0
            };

            volatility += (int)Math.Round(
                Math.Max(
                    val1: 0,
                    val2: housingPressurePercent - 45) *
                0.35m);
            volatility += GetSeedJitterPoints(
                generationSeed: city.GenerationSeed,
                salt: "population-social-volatility",
                maxAbsPoints: 8);

            return Math.Clamp(
                value: volatility,
                min: 0,
                max: 100);
        }

        private static int CalculateFamilyFormationPercent(
            CityView city,
            int housingPressurePercent)
        {
            int familyFormation = 50;

            familyFormation += city.SizeTier switch
            {
                "Small" => -6,
                "Large" => +10,
                _ => 0
            };

            familyFormation += city.UrbanDensity switch
            {
                "Sparse" => +10,
                "Dense" => -12,
                _ => 0
            };

            familyFormation += city.DevelopmentLevel switch
            {
                "Struggling" => -6,
                "Advanced" => +6,
                _ => 0
            };

            familyFormation += city.PopulationOccupancyProfile switch
            {
                "Light" => +6,
                "High" => -8,
                _ => 0
            };

            familyFormation -= (int)Math.Round(
                Math.Max(
                    val1: 0,
                    val2: housingPressurePercent - 50) *
                0.45m);
            familyFormation += GetSeedJitterPoints(
                generationSeed: city.GenerationSeed,
                salt: "population-family-formation",
                maxAbsPoints: 5);

            return Math.Clamp(
                value: familyFormation,
                min: 0,
                max: 100);
        }

        private static bool TryValidateBootstrapSummary(
            Guid cityId,
            CityPopulationBootstrapSummaryDto summary,
            int plannedPeopleCount,
            int residentialCapacity,
            out string? failureReason)
        {
            if (summary.CityId != cityId)
            {
                failureReason = "Population summary returned a different city id.";
                return false;
            }

            if (summary.RequestedPeopleCount != plannedPeopleCount)
            {
                failureReason = "Population summary requested count does not match the launch request.";
                return false;
            }

            if (summary.GeneratedPeopleCount < 0 ||
                summary.HouseholdCount < 0 ||
                summary.HousedHouseholdCount < 0 ||
                summary.HomelessHouseholdCount < 0 ||
                summary.HousedPeopleCount < 0 ||
                summary.HomelessPeopleCount < 0)
            {
                failureReason = "Population summary contains negative counters.";
                return false;
            }

            if (summary.GeneratedPeopleCount > summary.RequestedPeopleCount)
            {
                failureReason = "Population summary generated more people than requested.";
                return false;
            }

            if (summary.GeneratedPeopleCount > 0 && summary.HouseholdCount <= 0)
            {
                failureReason = "Population summary generated residents without any households.";
                return false;
            }

            long peopleBreakdown = (long)summary.HousedPeopleCount + summary.HomelessPeopleCount;
            if (peopleBreakdown != summary.GeneratedPeopleCount)
            {
                failureReason = "Population summary people breakdown is inconsistent.";
                return false;
            }

            long householdBreakdown = (long)summary.HousedHouseholdCount + summary.HomelessHouseholdCount;
            if (householdBreakdown != summary.HouseholdCount)
            {
                failureReason = "Population summary household breakdown is inconsistent.";
                return false;
            }

            if (summary.HousedPeopleCount > residentialCapacity)
            {
                failureReason = "Population summary reports more housed people than capacity allows.";
                return false;
            }

            if (summary.GeneratedPeopleCount == 0 && plannedPeopleCount > 0)
            {
                failureReason = "Population summary generated zero residents despite a non-zero plan.";
                return false;
            }

            failureReason = null;
            return true;
        }

        private static decimal GetBaseOccupancy(string occupancyProfile)
        {
            return occupancyProfile switch
            {
                "Light" => 0.44m,
                "High" => 0.82m,
                _ => 0.63m
            };
        }

        private static decimal GetDensityAdjustment(string urbanDensity)
        {
            return urbanDensity switch
            {
                "Sparse" => -0.06m,
                "Dense" => 0.06m,
                _ => 0.0m
            };
        }

        private static decimal GetDevelopmentAdjustment(string developmentLevel)
        {
            return developmentLevel switch
            {
                "Struggling" => -0.05m,
                "Advanced" => 0.04m,
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

        private static int GetSeedJitterPoints(
            string generationSeed,
            string salt,
            int maxAbsPoints)
        {
            if (maxAbsPoints <= 0)
                return 0;

            byte[] hash = SHA256.HashData(source: Encoding.UTF8.GetBytes($"{generationSeed}|{salt}"));
            int sample = BitConverter.ToInt32(
                             value: hash,
                             startIndex: 0) &
                         int.MaxValue;

            decimal normalized = sample / (decimal)int.MaxValue;
            decimal centered = (normalized - 0.5m) * 2m;

            return (int)Math.Round(
                d: centered * maxAbsPoints,
                mode: MidpointRounding.AwayFromZero);
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

        private static string DetermineEconomyFailureCode(Exception exception)
        {
            return exception switch
            {
                DownstreamServiceException downstreamException when
                    TryReadDownstreamErrorCode(
                        exception: downstreamException,
                        errorCode: out string? errorCode) &&
                    errorCode is "Gateway.InvalidDownstreamResponse" or "Gateway.InvalidDownstreamJson" =>
                    EconomyBootstrapFailureCodes.EconomyResponseInvalid,
                DownstreamServiceException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                    EconomyBootstrapFailureCodes.EconomyValidationFailed,
                DownstreamServiceException downstreamException when downstreamException.StatusCode ==
                                                                    HttpStatusCode.Conflict =>
                    EconomyBootstrapFailureCodes.EconomyConflict,
                DownstreamServiceException downstreamException when downstreamException.StatusCode ==
                                                                    HttpStatusCode.NotFound =>
                    EconomyBootstrapFailureCodes.EconomyDependencyNotFound,
                DownstreamServiceException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                    EconomyBootstrapFailureCodes.EconomyTimeout,
                DownstreamServiceException downstreamException when (int)downstreamException.StatusCode >= 500 =>
                    EconomyBootstrapFailureCodes.EconomyServiceUnavailable,
                HttpRequestException => EconomyBootstrapFailureCodes.EconomyTransportError,
                OperationCanceledException => EconomyBootstrapFailureCodes.EconomyTimeout,
                _ => EconomyBootstrapFailureCodes.EconomyUnexpectedError
            };
        }

        private static CityEconomyBootstrapView BuildFailedEconomyBootstrap(
            Guid operationId,
            string failureCode)
        {
            return new CityEconomyBootstrapView(
                OperationId: operationId,
                Status: EconomyBootstrapStatuses.Failed,
                FailureCode: failureCode,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);
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

        private async Task<bool> SupportsAutomaticPopulationBootstrapAsync(
            string simulationKind,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationKindCatalogItemView> supportedKinds =
                await citiesApiClient.GetSimulationKindsAsync(cancellationToken);

            SimulationKindCatalogItemView? descriptor = supportedKinds.FirstOrDefault(item =>
                string.Equals(
                    a: item.Kind,
                    b: simulationKind,
                    comparisonType: StringComparison.OrdinalIgnoreCase));

            if (descriptor is null)
            {
                logger.LogWarning(
                    message:
                    "Simulation kind metadata was not found for kind '{SimulationKind}'. Automatic population bootstrap will be skipped.",
                    simulationKind);

                return false;
            }

            return descriptor.SupportsAutomaticPopulationBootstrap;
        }

        private static class PopulationBootstrapStatuses
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string Skipped = "Skipped";
        }

        private static class EconomyBootstrapStatuses
        {
            public const string Completed = "Completed";
            public const string Failed = "Failed";
        }
    }
}
