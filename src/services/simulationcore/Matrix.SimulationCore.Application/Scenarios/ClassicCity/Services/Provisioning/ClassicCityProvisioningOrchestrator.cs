using System.Net;
using System.Security.Cryptography;
using System.Text;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;
using CityEconomyBootstrapView =
    Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityEconomyBootstrapModel;
using CityPopulationBootstrapSummaryView =
    Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityPopulationBootstrapSummaryModel;
using CityPopulationBootstrapView =
    Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityPopulationBootstrapModel;
using CityProvisioningView =
    Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityProvisioningModel;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning
{
    public sealed class ClassicCityProvisioningOrchestrator(
        IMediator mediator,
        ICityRepository cityRepository,
        ICityAnchorRepository cityAnchorRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ISimulationClockRepository clockRepository,
        IEnumerable<ICitySimulationBootstrapStrategy> simulationBootstrapStrategies,
        ICityEconomyBootstrapClient economyBootstrapClient,
        ICityPopulationBootstrapClient populationBootstrapClient,
        ILogger<ClassicCityProvisioningOrchestrator> logger) : IClassicCityProvisioningOrchestrator
    {
        public async Task<CityProvisioningView> CreateAsync(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            CityCreatedDto created = await mediator.Send(
                request: request,
                cancellationToken: cancellationToken);

            return await GetProvisioningViewAsync(
                cityId: created.CityId,
                cancellationToken: cancellationToken);
        }

        public async Task<CityProvisioningView> GetProvisioningViewAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            City city = await GetCityOrThrowAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return BuildProvisioningViewFromState(city);
        }

        public async Task<CityProvisioningView> ProvisionAsync(
            Guid cityId,
            string simulationKind,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            int? plannedPeopleCountOverride,
            Func<CancellationToken, Task>? heartbeatAsync,
            CancellationToken cancellationToken)
        {
            City city = await GetCityOrThrowAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (heartbeatAsync is not null)
                await heartbeatAsync(cancellationToken);

            CityEconomyBootstrapView economyBootstrap = await EnsureEconomyBootstrapAsync(
                city: city,
                simulationKind: simulationKind,
                operationId: economyBootstrapOperationId,
                cancellationToken: cancellationToken);

            if (!string.Equals(
                    a: economyBootstrap.Status,
                    b: EconomyBootstrapStatuses.Completed,
                    comparisonType: StringComparison.Ordinal))
                return new CityProvisioningView(
                    CityId: cityId,
                    SimulationKind: simulationKind,
                    PopulationBootstrap: BuildPopulationBootstrapFromState(
                        city: city,
                        operationId: populationBootstrapOperationId,
                        plannedPeopleCountOverride: plannedPeopleCountOverride),
                    EconomyBootstrap: economyBootstrap);

            if (!SupportsAutomaticPopulationBootstrap(simulationKind))
                return new CityProvisioningView(
                    CityId: cityId,
                    SimulationKind: simulationKind,
                    PopulationBootstrap: new CityPopulationBootstrapView(
                        OperationId: populationBootstrapOperationId,
                        Status: PopulationBootstrapStatuses.Skipped,
                        PlannedPeopleCount: plannedPeopleCountOverride ?? city.GenerationProfile.PlannedPeopleCount,
                        ResidentialCapacity: null,
                        Summary: null,
                        FailureCode: null),
                    EconomyBootstrap: economyBootstrap);

            if (heartbeatAsync is not null)
                await heartbeatAsync(cancellationToken);

            CityPopulationBootstrapView populationBootstrap = await EnsurePopulationBootstrapAsync(
                city: city,
                operationId: populationBootstrapOperationId,
                plannedPeopleCountOverride: plannedPeopleCountOverride,
                cancellationToken: cancellationToken);

            return new CityProvisioningView(
                CityId: cityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: populationBootstrap,
                EconomyBootstrap: economyBootstrap);
        }

        private CityProvisioningView BuildProvisioningViewFromState(City city)
        {
            string simulationKind = SimulationKind.ClassicCity.ToString();
            CityEconomyBootstrapView economyBootstrap = BuildEconomyBootstrapFromState(
                city: city,
                operationId: city.EconomyBootstrapOperationId);

            if (!SupportsAutomaticPopulationBootstrap(simulationKind))
                return new CityProvisioningView(
                    CityId: city.Id.Value,
                    SimulationKind: simulationKind,
                    PopulationBootstrap: new CityPopulationBootstrapView(
                        OperationId: city.PopulationBootstrapOperationId,
                        Status: PopulationBootstrapStatuses.Skipped,
                        PlannedPeopleCount: city.GenerationProfile.PlannedPeopleCount,
                        ResidentialCapacity: null,
                        Summary: null,
                        FailureCode: null),
                    EconomyBootstrap: economyBootstrap);

            return new CityProvisioningView(
                CityId: city.Id.Value,
                SimulationKind: simulationKind,
                PopulationBootstrap: BuildPopulationBootstrapFromState(
                    city: city,
                    operationId: city.PopulationBootstrapOperationId,
                    plannedPeopleCountOverride: city.GenerationProfile.PlannedPeopleCount),
                EconomyBootstrap: economyBootstrap);
        }

        private async Task<CityEconomyBootstrapView> EnsureEconomyBootstrapAsync(
            City city,
            string simulationKind,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            if (city.EconomyBootstrapCompletedAtUtc.HasValue || city.IsActive)
                return BuildEconomyBootstrapFromState(
                    city: city,
                    operationId: operationId);

            if (city.EconomyBootstrapFailedAtUtc.HasValue)
                return BuildEconomyBootstrapFromState(
                    city: city,
                    operationId: operationId);

            CityEconomyBootstrapView bootstrap = await BootstrapEconomyAsync(
                city: city,
                simulationKind: simulationKind,
                operationId: operationId,
                cancellationToken: cancellationToken);

            if (string.Equals(
                    a: bootstrap.Status,
                    b: EconomyBootstrapStatuses.Completed,
                    comparisonType: StringComparison.Ordinal))
                await mediator.Send(
                    request: new CompleteCityEconomyBootstrapCommand(
                        CityId: city.Id.Value,
                        OperationId: operationId),
                    cancellationToken: cancellationToken);
            else
                await mediator.Send(
                    request: new FailCityEconomyBootstrapCommand(
                        CityId: city.Id.Value,
                        OperationId: operationId,
                        FailureCode: bootstrap.FailureCode ?? EconomyBootstrapFailureCodes.EconomyUnexpectedError),
                    cancellationToken: cancellationToken);

            return bootstrap;
        }

        private async Task<CityPopulationBootstrapView> EnsurePopulationBootstrapAsync(
            City city,
            Guid operationId,
            int? plannedPeopleCountOverride,
            CancellationToken cancellationToken)
        {
            if (city.PopulationBootstrapCompletedAtUtc.HasValue || city.IsActive)
                return BuildPopulationBootstrapFromState(
                    city: city,
                    operationId: operationId,
                    plannedPeopleCountOverride: plannedPeopleCountOverride);

            if (city.PopulationBootstrapFailedAtUtc.HasValue)
                return BuildPopulationBootstrapFromState(
                    city: city,
                    operationId: operationId,
                    plannedPeopleCountOverride: plannedPeopleCountOverride);

            CityPopulationBootstrapView bootstrap = await BootstrapPopulationAsync(
                city: city,
                operationId: operationId,
                plannedPeopleCountOverride: plannedPeopleCountOverride,
                cancellationToken: cancellationToken);

            if (string.Equals(
                    a: bootstrap.Status,
                    b: PopulationBootstrapStatuses.Completed,
                    comparisonType: StringComparison.Ordinal))
                await mediator.Send(
                    request: new CompleteCityPopulationBootstrapCommand(
                        CityId: city.Id.Value,
                        OperationId: operationId),
                    cancellationToken: cancellationToken);
            else
                await mediator.Send(
                    request: new FailCityPopulationBootstrapCommand(
                        CityId: city.Id.Value,
                        OperationId: operationId,
                        FailureCode: bootstrap.FailureCode ??
                                     PopulationBootstrapFailureCodes.PopulationUnexpectedError),
                    cancellationToken: cancellationToken);

            return bootstrap;
        }

        private async Task<CityEconomyBootstrapView> BootstrapEconomyAsync(
            City city,
            string simulationKind,
            Guid operationId,
            CancellationToken cancellationToken)
        {
            try
            {
                CityEconomyBootstrapResult result = await economyBootstrapClient.InitializeAsync(
                    cityId: city.Id.Value,
                    simulationKind: simulationKind,
                    economyProfile: city.GenerationProfile.EconomyProfile.ToString(),
                    createdAtUtc: city.CreatedAtUtc,
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
                    city.Id.Value);

                return BuildFailedEconomyBootstrap(
                    operationId: operationId,
                    failureCode: DetermineEconomyFailureCode(ex));
            }
        }

        private async Task<CityPopulationBootstrapView> BootstrapPopulationAsync(
            City city,
            Guid operationId,
            int? plannedPeopleCountOverride,
            CancellationToken cancellationToken)
        {
            int? plannedPeopleCount = null;
            int? residentialCapacity = null;

            try
            {
                // These repositories share one scoped DbContext, so querying them in
                // parallel during provisioning can trigger EF Core concurrency errors.
                SimulationClock? clock = await clockRepository.GetBySimulationIdAsync(
                    simulationId: new SimulationId(city.Id.Value),
                    cancellationToken: cancellationToken);
                IReadOnlyList<CityAnchor> anchors = await cityAnchorRepository.ListByCityIdAsync(
                    cityId: city.Id,
                    cancellationToken: cancellationToken);
                IReadOnlyList<ResidentialBuilding> buildings = await residentialBuildingRepository.ListByCityIdAsync(
                    cityId: city.Id,
                    districtId: null,
                    cancellationToken: cancellationToken);

                if (clock is null)
                    throw new InvalidOperationException($"Simulation clock is missing for cityId={city.Id.Value}.");

                residentialCapacity = buildings.Sum(x => x.ResidentCapacity.Value);
                plannedPeopleCount = ResolvePlannedPeopleCount(
                    plannedPeopleCountOverride: plannedPeopleCountOverride,
                    city: city,
                    buildings: buildings);

                if (residentialCapacity <= 0)
                {
                    logger.LogWarning(
                        message:
                        "Automatic population bootstrap aborted for cityId={CityId} because generated topology has no residential capacity.",
                        city.Id.Value);

                    return new CityPopulationBootstrapView(
                        OperationId: operationId,
                        Status: PopulationBootstrapStatuses.Failed,
                        PlannedPeopleCount: plannedPeopleCount,
                        ResidentialCapacity: residentialCapacity,
                        Summary: null,
                        FailureCode: PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing);
                }

                CityPopulationBootstrapSummary summary = await populationBootstrapClient.InitializeAsync(
                    request: new CityPopulationBootstrapInitializationRequest(
                        CityId: city.Id.Value,
                        CurrentDate: DateOnly.FromDateTime(clock.CurrentTime.ValueUtc.UtcDateTime),
                        CreatedAtUtc: clock.CurrentTime.ValueUtc,
                        PeopleCount: plannedPeopleCount!.Value,
                        RandomSeed: BuildPopulationRandomSeed(city.GenerationSeed.Value),
                        Environment: new CityPopulationBootstrapEnvironment(
                            ClimateZone: city.Environment.ClimateZone.ToString(),
                            Hemisphere: city.Environment.Hemisphere.ToString(),
                            UtcOffsetMinutes: city.Environment.UtcOffset.TotalMinutes),
                        Tuning: BuildBootstrapTuning(
                            city: city,
                            plannedPeopleCount: plannedPeopleCount.Value,
                            residentialCapacity: residentialCapacity.Value),
                        CityAnchors: anchors
                           .Select(x => new CityAnchorSeed(
                                CityAnchorId: x.Id.Value,
                                DistrictId: x.DistrictId.Value,
                                AccessRoadNodeId: x.AccessRoadNodeId.Value,
                                Name: x.Name.Value,
                                Type: x.Type.ToString(),
                                Capacity: x.Capacity,
                                PositionX: x.PositionX,
                                PositionY: x.PositionY,
                                CreatedAtUtc: x.CreatedAtUtc))
                           .ToArray(),
                        ResidentialBuildings: buildings
                           .Select(x => new ResidentialBuildingSeed(
                                ResidentialBuildingId: x.Id.Value,
                                DistrictId: x.DistrictId.Value,
                                ResidentCapacity: x.ResidentCapacity.Value))
                           .ToArray()),
                    cancellationToken: cancellationToken);

                if (!TryValidateBootstrapSummary(
                        cityId: city.Id.Value,
                        summary: summary,
                        plannedPeopleCount: plannedPeopleCount.Value,
                        residentialCapacity: residentialCapacity.Value,
                        failureReason: out string? failureReason))
                {
                    logger.LogWarning(
                        message:
                        "Automatic population bootstrap returned an inconsistent summary for cityId={CityId}. Reason: {FailureReason}",
                        city.Id.Value,
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
                    Summary: MapSummary(summary),
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
                    city.Id.Value);

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
                    city.Id.Value);

                return new CityPopulationBootstrapView(
                    OperationId: operationId,
                    Status: PopulationBootstrapStatuses.Failed,
                    PlannedPeopleCount: plannedPeopleCount,
                    ResidentialCapacity: residentialCapacity,
                    Summary: null,
                    FailureCode: DeterminePopulationFailureCode(ex));
            }
        }

        private bool SupportsAutomaticPopulationBootstrap(string simulationKind)
        {
            SimulationKind parsedKind = Enum.Parse<SimulationKind>(
                value: simulationKind,
                ignoreCase: true);
            ICitySimulationBootstrapStrategy strategy = ResolveBootstrapStrategy(parsedKind);
            return strategy.Descriptor.SupportsAutomaticPopulationBootstrap;
        }

        private ICitySimulationBootstrapStrategy ResolveBootstrapStrategy(SimulationKind simulationKind)
        {
            ICitySimulationBootstrapStrategy? strategy =
                simulationBootstrapStrategies.SingleOrDefault(x => x.Kind == simulationKind);

            return strategy ??
                   throw new InvalidOperationException(
                       $"City simulation bootstrap strategy is not registered for kind '{simulationKind}'.");
        }

        private async Task<City> GetCityOrThrowAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(cityId),
                cancellationToken: cancellationToken);

            return city ??
                   throw new InvalidOperationException(
                       $"City '{cityId}' was not found after provisioning orchestration started.");
        }

        private static CityEconomyBootstrapView BuildEconomyBootstrapFromState(
            City city,
            Guid operationId)
        {
            bool failed = city.EconomyBootstrapFailedAtUtc.HasValue;
            bool completed = city.EconomyBootstrapCompletedAtUtc.HasValue || city.IsActive || city.IsArchived;

            return new CityEconomyBootstrapView(
                OperationId: operationId,
                Status: failed
                    ? EconomyBootstrapStatuses.Failed
                    : completed
                        ? EconomyBootstrapStatuses.Completed
                        : EconomyBootstrapStatuses.Pending,
                FailureCode: failed
                    ? city.EconomyBootstrapFailureCode
                    : null,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);
        }

        private static CityPopulationBootstrapView BuildPopulationBootstrapFromState(
            City city,
            Guid operationId,
            int? plannedPeopleCountOverride)
        {
            bool failed = city.PopulationBootstrapFailedAtUtc.HasValue;
            bool completed = city.PopulationBootstrapCompletedAtUtc.HasValue || city.IsActive || city.IsArchived;

            return new CityPopulationBootstrapView(
                OperationId: operationId,
                Status: failed
                    ? PopulationBootstrapStatuses.Failed
                    : completed
                        ? PopulationBootstrapStatuses.Completed
                        : PopulationBootstrapStatuses.Pending,
                PlannedPeopleCount: plannedPeopleCountOverride ?? city.GenerationProfile.PlannedPeopleCount,
                ResidentialCapacity: null,
                Summary: null,
                FailureCode: failed
                    ? city.PopulationBootstrapFailureCode
                    : null);
        }

        private static CityPopulationBootstrapSummaryView MapSummary(CityPopulationBootstrapSummary summary)
        {
            return new CityPopulationBootstrapSummaryView(
                CityId: summary.CityId,
                RequestedPeopleCount: summary.RequestedPeopleCount,
                GeneratedPeopleCount: summary.GeneratedPeopleCount,
                HouseholdCount: summary.HouseholdCount,
                HousedHouseholdCount: summary.HousedHouseholdCount,
                HomelessHouseholdCount: summary.HomelessHouseholdCount,
                HousedPeopleCount: summary.HousedPeopleCount,
                HomelessPeopleCount: summary.HomelessPeopleCount);
        }

        private static int ResolvePlannedPeopleCount(
            int? plannedPeopleCountOverride,
            City city,
            IReadOnlyList<ResidentialBuilding> buildings)
        {
            int totalCapacity = buildings.Sum(x => x.ResidentCapacity.Value);

            if (plannedPeopleCountOverride.HasValue)
                return totalCapacity <= 0
                    ? plannedPeopleCountOverride.Value
                    : Math.Clamp(
                        value: plannedPeopleCountOverride.Value,
                        min: 0,
                        max: totalCapacity);

            if (city.GenerationProfile.PlannedPeopleCount.HasValue)
                return totalCapacity <= 0
                    ? city.GenerationProfile.PlannedPeopleCount.Value
                    : Math.Clamp(
                        value: city.GenerationProfile.PlannedPeopleCount.Value,
                        min: 0,
                        max: totalCapacity);

            if (totalCapacity <= 0)
                return 0;

            decimal occupancyRate = GetBaseOccupancy(city.GenerationProfile.PopulationOccupancyProfile.ToString()) +
                                    GetDensityAdjustment(city.GenerationProfile.UrbanDensity.ToString()) +
                                    GetDevelopmentAdjustment(city.GenerationProfile.DevelopmentLevel.ToString()) +
                                    GetSeedJitter(city.GenerationSeed.Value);

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

        private static CityPopulationBootstrapTuning BuildBootstrapTuning(
            City city,
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

            return new CityPopulationBootstrapTuning(
                HousingPressurePercent: housingPressurePercent,
                EconomicStabilityPercent: economicStabilityPercent,
                SocialVolatilityPercent: socialVolatilityPercent,
                FamilyFormationPercent: familyFormationPercent);
        }

        private static int CalculateHousingPressurePercent(
            City city,
            int plannedPeopleCount,
            int residentialCapacity)
        {
            decimal occupancyRatio = residentialCapacity <= 0
                ? 1.20m
                : plannedPeopleCount / (decimal)residentialCapacity;

            int pressure = 45 + (int)Math.Round((occupancyRatio - 0.70m) * 110m);

            pressure += city.GenerationProfile.UrbanDensity.ToString() switch
            {
                "Sparse" => -10,
                "Dense" => +12,
                _ => 0
            };

            pressure += city.GenerationProfile.DevelopmentLevel.ToString() switch
            {
                "Struggling" => +10,
                "Advanced" => -8,
                _ => 0
            };

            pressure += city.GenerationProfile.PopulationOccupancyProfile.ToString() switch
            {
                "Light" => -8,
                "High" => +10,
                _ => 0
            };

            pressure += city.GenerationProfile.SizeTier.ToString() switch
            {
                "Small" => +4,
                "Large" => -3,
                _ => 0
            };

            pressure += GetSeedJitterPoints(
                generationSeed: city.GenerationSeed.Value,
                salt: "population-housing-pressure",
                maxAbsPoints: 5);

            return Math.Clamp(
                value: pressure,
                min: 0,
                max: 100);
        }

        private static int CalculateEconomicStabilityPercent(
            City city,
            int housingPressurePercent)
        {
            int stability = 52;

            stability += city.GenerationProfile.DevelopmentLevel.ToString() switch
            {
                "Struggling" => -18,
                "Advanced" => +16,
                _ => 0
            };

            stability += city.GenerationProfile.UrbanDensity.ToString() switch
            {
                "Sparse" => -4,
                "Dense" => +4,
                _ => 0
            };

            stability += city.GenerationProfile.SizeTier.ToString() switch
            {
                "Small" => -4,
                "Large" => +6,
                _ => 0
            };

            stability += city.GenerationProfile.PopulationOccupancyProfile.ToString() switch
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
                generationSeed: city.GenerationSeed.Value,
                salt: "population-economic-stability",
                maxAbsPoints: 6);

            return Math.Clamp(
                value: stability,
                min: 0,
                max: 100);
        }

        private static int CalculateSocialVolatilityPercent(
            City city,
            int housingPressurePercent)
        {
            int volatility = 40;

            volatility += city.GenerationProfile.UrbanDensity.ToString() switch
            {
                "Sparse" => -8,
                "Dense" => +14,
                _ => 0
            };

            volatility += city.GenerationProfile.DevelopmentLevel.ToString() switch
            {
                "Struggling" => +12,
                "Advanced" => -6,
                _ => 0
            };

            volatility += city.GenerationProfile.PopulationOccupancyProfile.ToString() switch
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
                generationSeed: city.GenerationSeed.Value,
                salt: "population-social-volatility",
                maxAbsPoints: 8);

            return Math.Clamp(
                value: volatility,
                min: 0,
                max: 100);
        }

        private static int CalculateFamilyFormationPercent(
            City city,
            int housingPressurePercent)
        {
            int familyFormation = 50;

            familyFormation += city.GenerationProfile.SizeTier.ToString() switch
            {
                "Small" => -6,
                "Large" => +10,
                _ => 0
            };

            familyFormation += city.GenerationProfile.UrbanDensity.ToString() switch
            {
                "Sparse" => +10,
                "Dense" => -12,
                _ => 0
            };

            familyFormation += city.GenerationProfile.DevelopmentLevel.ToString() switch
            {
                "Struggling" => -6,
                "Advanced" => +6,
                _ => 0
            };

            familyFormation += city.GenerationProfile.PopulationOccupancyProfile.ToString() switch
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
                generationSeed: city.GenerationSeed.Value,
                salt: "population-family-formation",
                maxAbsPoints: 5);

            return Math.Clamp(
                value: familyFormation,
                min: 0,
                max: 100);
        }

        private static bool TryValidateBootstrapSummary(
            Guid cityId,
            CityPopulationBootstrapSummary summary,
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

        private static string DeterminePopulationFailureCode(Exception exception)
        {
            return exception switch
            {
                InvalidOperationException => PopulationBootstrapFailureCodes.PopulationResponseInvalid,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                    PopulationBootstrapFailureCodes.PopulationValidationFailed,
                HttpRequestException downstreamException when downstreamException.StatusCode == HttpStatusCode.Conflict
                    =>
                    PopulationBootstrapFailureCodes.PopulationConflict,
                HttpRequestException downstreamException when downstreamException.StatusCode == HttpStatusCode.NotFound
                    =>
                    PopulationBootstrapFailureCodes.PopulationDependencyNotFound,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                    PopulationBootstrapFailureCodes.PopulationTimeout,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode.HasValue && (int)downstreamException.StatusCode.Value >= 500 =>
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
                InvalidOperationException => EconomyBootstrapFailureCodes.EconomyResponseInvalid,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                    EconomyBootstrapFailureCodes.EconomyValidationFailed,
                HttpRequestException downstreamException when downstreamException.StatusCode == HttpStatusCode.Conflict
                    =>
                    EconomyBootstrapFailureCodes.EconomyConflict,
                HttpRequestException downstreamException when downstreamException.StatusCode == HttpStatusCode.NotFound
                    =>
                    EconomyBootstrapFailureCodes.EconomyDependencyNotFound,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout =>
                    EconomyBootstrapFailureCodes.EconomyTimeout,
                HttpRequestException downstreamException when
                    downstreamException.StatusCode.HasValue && (int)downstreamException.StatusCode.Value >= 500 =>
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

        private static class PopulationBootstrapStatuses
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string Skipped = "Skipped";
        }

        private static class EconomyBootstrapStatuses
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
        }
    }
}
