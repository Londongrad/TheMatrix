using System.Net;
using MassTransit;
using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionService(
        IClassicCitySetupSessionStore sessionStore,
        ICitiesApiClient citiesApiClient,
        ICityProvisioningService provisioningService,
        IPublishEndpoint publishEndpoint,
        IOptions<ClassicCitySetupSessionOptions> options,
        ILogger<ClassicCitySetupSessionService> logger)
        : IClassicCitySetupSessionService
    {
        private const int MaxPlannedPeopleCount = 1_000_000;
        private const string PopulationOccupancyProfileLight = "Light";
        private const string PopulationOccupancyProfileBalanced = "Balanced";
        private const string PopulationOccupancyProfileHigh = "High";
        private const string PopulationBootstrapStatusPending = "Pending";
        private const string PopulationBootstrapStatusCompleted = "Completed";
        private const string PopulationBootstrapStatusFailed = "Failed";
        private readonly ClassicCitySetupSessionOptions _options = options.Value;

        private static readonly string[] MutableStatuses =
        [
            ClassicCitySetupSessionStatuses.Draft,
            ClassicCitySetupSessionStatuses.LaunchFailed
        ];

        public async Task<ClassicCitySetupSessionView> CreateAsync(
            CreateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            var session = new ClassicCitySetupSessionState
            {
                SessionId = Guid.NewGuid(),
                ScenarioKind = "ClassicCity",
                Status = ClassicCitySetupSessionStatuses.Draft,
                CurrentStepId = NormalizeStepId(request.CurrentStepId),
                Draft = NormalizeDraft(request.Draft),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            return MapToView(session);
        }

        public async Task<ClassicCitySetupSessionView?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                sessionId: sessionId,
                cancellationToken: cancellationToken);

            return session is null
                ? null
                : MapToView(session);
        }

        public async Task<ClassicCitySetupSessionMutationResult> UpdateAsync(
            Guid sessionId,
            UpdateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return await ExecuteMutationAsync(
                sessionId: sessionId,
                unavailableFallbackMessage: "Setup session is temporarily unavailable for editing.",
                action: async () =>
                {
                    ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                        sessionId: sessionId,
                        cancellationToken: cancellationToken);

                    if (session is null)
                        return NotFound();

                    if (!MutableStatuses.Contains(session.Status, StringComparer.Ordinal))
                        return Conflict(
                            session,
                            code: ClassicCitySetupSessionFailureCodes.InvalidLaunchState,
                            message: "This setup session can no longer be edited because launch orchestration is already in progress or completed.");

                    session.CurrentStepId = NormalizeStepId(request.CurrentStepId);
                    session.Draft = NormalizeDraft(request.Draft);
                    session.UpdatedAtUtc = DateTimeOffset.UtcNow;

                    await sessionStore.SaveAsync(
                        session: session,
                        cancellationToken: cancellationToken);

                    return Updated(session);
                },
                cancellationToken: cancellationToken);
        }

        public async Task<ClassicCitySetupSessionMutationResult> QueueLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteMutationAsync(
                sessionId: sessionId,
                unavailableFallbackMessage: "Setup session is temporarily unavailable for launch orchestration.",
                action: async () =>
                {
                    ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                        sessionId: sessionId,
                        cancellationToken: cancellationToken);

                    if (session is null)
                        return NotFound();

                    if (!MutableStatuses.Contains(session.Status, StringComparer.Ordinal))
                        return Conflict(
                            session,
                            code: ClassicCitySetupSessionFailureCodes.InvalidLaunchState,
                            message: "This setup session is already queued, running, or attached to a launched city.");

                    ClassicCitySetupDraftDto draft = NormalizeDraft(session.Draft);

                    if (!TryBuildLaunchRequest(
                            draft: draft,
                            launchRequest: out CreateCityRequestDto? launchRequest,
                            errorMessage: out string? errorMessage))
                    {
                        session.Draft = draft;
                        session.UpdatedAtUtc = DateTimeOffset.UtcNow;

                        await sessionStore.SaveAsync(
                            session: session,
                            cancellationToken: cancellationToken);

                        return Invalid(
                            session,
                            code: "Gateway.ClassicCitySetup.ValidationFailed",
                            message: errorMessage ?? "Setup draft is incomplete.");
                    }

                    launchRequest = launchRequest! with
                    {
                        ProvisioningCorrelationId = session.SessionId
                    };

                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    session.Status = ClassicCitySetupSessionStatuses.LaunchQueued;
                    session.CurrentStepId = ClassicCitySetupSteps.Launch;
                    session.Draft = draft;
                    session.LaunchRequest = launchRequest;
                    session.FailureCode = null;
                    session.FailureMessage = null;
                    session.CityId = null;
                    session.SimulationKind = null;
                    session.Provisioning = null;
                    session.LaunchQueuedAtUtc = now;
                    session.StartedAtUtc = null;
                    session.CompletedAtUtc = null;
                    session.UpdatedAtUtc = now;

                    await sessionStore.SaveAsync(
                        session: session,
                        cancellationToken: cancellationToken);

                    try
                    {
                        await publishEndpoint.Publish(
                            message: new ClassicCitySetupLaunchRequested(session.SessionId),
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            exception: ex,
                            message: "Classic City setup launch could not be queued for sessionId={SessionId}.",
                            session.SessionId);

                        await FailLaunchAsync(
                            session: session,
                            failureCode: ClassicCitySetupSessionFailureCodes.LaunchQueueUnavailable,
                            failureMessage: BuildSafeFailureMessage(
                                exception: ex,
                                fallback: "Launch request could not be queued. Retry when gateway messaging is healthy."),
                            cancellationToken: cancellationToken);

                        return Unavailable(
                            session,
                            code: ClassicCitySetupSessionFailureCodes.LaunchQueueUnavailable,
                            message: session.FailureMessage ?? "Launch request could not be queued.");
                    }

                    return Updated(session);
                },
                cancellationToken: cancellationToken);
        }

        public async Task ProcessLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithSessionLockAsync(
                sessionId: sessionId,
                unavailableFallbackMessage: null,
                action: async session =>
                {
                    if (session is null ||
                        !string.Equals(
                            session.Status,
                            ClassicCitySetupSessionStatuses.LaunchQueued,
                            StringComparison.Ordinal))
                    {
                        return;
                    }

                    await ProcessLaunchCoreAsync(
                        session: session,
                        cancellationToken: cancellationToken);
                },
                cancellationToken: cancellationToken);
        }

        public async Task ReconcileAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithSessionLockAsync(
                sessionId: sessionId,
                unavailableFallbackMessage: null,
                action: async session =>
                {
                    if (session is null)
                    {
                        await sessionStore.UntrackAsync(
                            sessionId: sessionId,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (ShouldStopTracking(session.Status))
                    {
                        await sessionStore.UntrackAsync(
                            sessionId: sessionId,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (ShouldRecoverQueuedLaunch(session))
                    {
                        logger.LogInformation(
                            message: "Classic City setup session recovery is resuming a stale queued launch for sessionId={SessionId}.",
                            session.SessionId);

                        await ProcessLaunchCoreAsync(
                            session: session,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (ShouldRecoverCreatingCity(session))
                    {
                        logger.LogInformation(
                            message: "Classic City setup session recovery is replaying city creation for stale sessionId={SessionId}.",
                            session.SessionId);

                        await ProcessLaunchCoreAsync(
                            session: session,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (!session.CityId.HasValue)
                    {
                        if (IsCreatingCityWithoutCorrelationStale(session))
                        {
                            logger.LogWarning(
                                message:
                                "Classic City setup session reconciliation detected a stale creating-city state without a city correlation for sessionId={SessionId}. Manual review may be required.",
                                session.SessionId);
                        }

                        return;
                    }

                    await ReconcileProvisioningAsync(
                        session: session,
                        cancellationToken: cancellationToken);
                },
                cancellationToken: cancellationToken);
        }

        private static ClassicCitySetupDraftDto NormalizeDraft(ClassicCitySetupDraftDto draft)
        {
            ArgumentNullException.ThrowIfNull(draft);

            return draft with
            {
                Name = draft.Name?.Trim() ?? string.Empty,
                StartSimTimeLocal = draft.StartSimTimeLocal?.Trim() ?? string.Empty,
                SpeedMultiplier = draft.SpeedMultiplier?.Trim() ?? string.Empty,
                ClimateZone = string.IsNullOrWhiteSpace(draft.ClimateZone) ? "Temperate" : draft.ClimateZone.Trim(),
                Hemisphere = string.IsNullOrWhiteSpace(draft.Hemisphere) ? "Northern" : draft.Hemisphere.Trim(),
                UtcOffsetMinutes = draft.UtcOffsetMinutes?.Trim() ?? string.Empty,
                GenerationSeed = draft.GenerationSeed?.Trim() ?? string.Empty,
                SizeTier = string.IsNullOrWhiteSpace(draft.SizeTier) ? "Medium" : draft.SizeTier.Trim(),
                UrbanDensity = string.IsNullOrWhiteSpace(draft.UrbanDensity) ? "Balanced" : draft.UrbanDensity.Trim(),
                DevelopmentLevel = string.IsNullOrWhiteSpace(draft.DevelopmentLevel) ? "Balanced" : draft.DevelopmentLevel.Trim(),
                PopulationOccupancyProfile = NormalizePopulationOccupancyProfile(draft.PopulationOccupancyProfile),
                UsePopulationOverride = draft.UsePopulationOverride,
                PlannedPeopleCount = draft.PlannedPeopleCount?.Trim() ?? string.Empty
            };
        }

        private static string NormalizeStepId(string? currentStepId)
        {
            string normalized = currentStepId?.Trim().ToLowerInvariant() ?? ClassicCitySetupSteps.Scenario;
            return ClassicCitySetupSteps.IsKnown(normalized)
                ? normalized
                : ClassicCitySetupSteps.Scenario;
        }

        private static bool TryBuildLaunchRequest(
            ClassicCitySetupDraftDto draft,
            out CreateCityRequestDto? launchRequest,
            out string? errorMessage)
        {
            launchRequest = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(draft.Name))
            {
                errorMessage = "City name is required before launch.";
                return false;
            }

            if (!draft.StartSimTimeUtc.HasValue)
            {
                errorMessage = "Start simulation time is invalid or missing.";
                return false;
            }

            if (!decimal.TryParse(
                    s: draft.SpeedMultiplier,
                    style: System.Globalization.NumberStyles.Number,
                    provider: System.Globalization.CultureInfo.InvariantCulture,
                    result: out decimal speedMultiplier) ||
                speedMultiplier <= 0)
            {
                errorMessage = "Speed multiplier must be a number greater than 0.";
                return false;
            }

            if (!int.TryParse(
                    s: draft.UtcOffsetMinutes,
                    style: System.Globalization.NumberStyles.Integer,
                    provider: System.Globalization.CultureInfo.InvariantCulture,
                    result: out int utcOffsetMinutes) ||
                utcOffsetMinutes < -14 * 60 ||
                utcOffsetMinutes > 14 * 60)
            {
                errorMessage = "UTC offset must stay between -840 and 840 minutes.";
                return false;
            }

            int? plannedPeopleCount = null;

            if (draft.UsePopulationOverride)
            {
                if (!int.TryParse(
                        s: draft.PlannedPeopleCount,
                        style: System.Globalization.NumberStyles.Integer,
                        provider: System.Globalization.CultureInfo.InvariantCulture,
                        result: out int parsedPeopleCount) ||
                    parsedPeopleCount < 0 ||
                    parsedPeopleCount > MaxPlannedPeopleCount)
                {
                    errorMessage =
                        $"Planned people count must be a whole number between 0 and {MaxPlannedPeopleCount}.";
                    return false;
                }

                plannedPeopleCount = parsedPeopleCount;
            }

            launchRequest = new CreateCityRequestDto(
                Name: draft.Name,
                StartSimTimeUtc: draft.StartSimTimeUtc.Value,
                SpeedMultiplier: speedMultiplier,
                SimulationKind: "ClassicCity",
                ClimateZone: draft.ClimateZone,
                Hemisphere: draft.Hemisphere,
                UtcOffsetMinutes: utcOffsetMinutes,
                GenerationSeed: string.IsNullOrWhiteSpace(draft.GenerationSeed) ? null : draft.GenerationSeed,
                SizeTier: draft.SizeTier,
                UrbanDensity: draft.UrbanDensity,
                DevelopmentLevel: draft.DevelopmentLevel,
                PopulationOccupancyProfile: draft.PopulationOccupancyProfile,
                PlannedPeopleCount: plannedPeopleCount);

            return true;
        }

        private static string NormalizePopulationOccupancyProfile(string? value)
        {
            return value?.Trim() switch
            {
                PopulationOccupancyProfileLight => PopulationOccupancyProfileLight,
                PopulationOccupancyProfileHigh => PopulationOccupancyProfileHigh,
                _ => PopulationOccupancyProfileBalanced
            };
        }

        private static CityProvisioningView BuildPendingProvisioning(
            CityCreatedView created,
            int? plannedPeopleCount)
        {
            return new CityProvisioningView(
                CityId: created.CityId,
                SimulationKind: created.SimulationKind,
                PopulationBootstrap: new CityPopulationBootstrapView(
                    OperationId: created.PopulationBootstrapOperationId,
                    Status: "Pending",
                    PlannedPeopleCount: plannedPeopleCount,
                    ResidentialCapacity: null,
                    Summary: null,
                    FailureCode: null));
        }

        private static CityProvisioningView BuildFailedProvisioningFromPending(
            Guid cityId,
            string simulationKind,
            Guid operationId,
            string failureCode)
        {
            return new CityProvisioningView(
                CityId: cityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: new CityPopulationBootstrapView(
                    OperationId: operationId,
                    Status: "Failed",
                    PlannedPeopleCount: null,
                    ResidentialCapacity: null,
                    Summary: null,
                    FailureCode: failureCode));
        }

        private async Task ProcessLaunchCoreAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken)
        {
            if (session.LaunchRequest is null)
            {
                await FailLaunchAsync(
                    session: session,
                    failureCode: ClassicCitySetupSessionFailureCodes.LaunchRequestMissing,
                    failureMessage: "Setup session cannot start because the queued launch payload is missing.",
                    cancellationToken: cancellationToken);
                return;
            }

            DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
            session.Status = ClassicCitySetupSessionStatuses.CreatingCity;
            session.StartedAtUtc ??= startedAtUtc;
            session.UpdatedAtUtc = startedAtUtc;

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            CityCreatedView? created = null;

            try
            {
                created = await provisioningService.CreateCitySkeletonAsync(
                    request: session.LaunchRequest,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup launch failed during city creation for sessionId={SessionId}.",
                    session.SessionId);

                await FailLaunchAsync(
                    session: session,
                    failureCode: DetermineCityCreateFailureCode(ex),
                    failureMessage: BuildSafeFailureMessage(ex, "City creation failed before provisioning could start."),
                    cancellationToken: cancellationToken);
                return;
            }

            DateTimeOffset provisioningStartedAtUtc = DateTimeOffset.UtcNow;
            session.Status = ClassicCitySetupSessionStatuses.BootstrappingPopulation;
            session.CityId = created.CityId;
            session.SimulationKind = created.SimulationKind;
            session.Provisioning = BuildPendingProvisioning(
                created: created,
                plannedPeopleCount: session.LaunchRequest.PlannedPeopleCount);
            session.UpdatedAtUtc = provisioningStartedAtUtc;

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            try
            {
                CityProvisioningView provisioning = await provisioningService.ProvisionCreatedCityAsync(
                    cityId: created.CityId,
                    simulationKind: created.SimulationKind,
                    operationId: created.PopulationBootstrapOperationId,
                    cancellationToken: cancellationToken);

                FinalizeFromProvisioning(session, provisioning);

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup launch failed after city creation for sessionId={SessionId} cityId={CityId}.",
                    session.SessionId,
                    created.CityId);

                session.Status = ClassicCitySetupSessionStatuses.ProvisioningFailed;
                session.Provisioning = BuildFailedProvisioningFromPending(
                    cityId: created.CityId,
                    simulationKind: created.SimulationKind,
                    operationId: created.PopulationBootstrapOperationId,
                    failureCode: ClassicCitySetupSessionFailureCodes.ProvisioningUnexpectedError);
                session.FailureCode = ClassicCitySetupSessionFailureCodes.ProvisioningUnexpectedError;
                session.FailureMessage = BuildSafeFailureMessage(
                    exception: ex,
                    fallback: "Population bootstrap finished with an unexpected orchestration error.");
                session.CompletedAtUtc = DateTimeOffset.UtcNow;
                session.UpdatedAtUtc = session.CompletedAtUtc.Value;

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ReconcileProvisioningAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken)
        {
            CityProvisioningStatusView provisioningStatus;

            try
            {
                provisioningStatus = await citiesApiClient.GetProvisioningStatusAsync(
                    cityId: session.CityId!.Value,
                    cancellationToken: cancellationToken);
            }
            catch (DownstreamServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                session.Status = ClassicCitySetupSessionStatuses.ProvisioningFailed;
                session.FailureCode = ClassicCitySetupSessionFailureCodes.ReconciliationCityNotFound;
                session.FailureMessage =
                    "Gateway could not find the city host during reconciliation. Operator review is required.";
                session.CompletedAtUtc = DateTimeOffset.UtcNow;
                session.UpdatedAtUtc = session.CompletedAtUtc.Value;

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);
                return;
            }

            ApplyProvisioningStatus(
                session: session,
                provisioningStatus: provisioningStatus);

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            if (string.Equals(session.Status, ClassicCitySetupSessionStatuses.Ready, StringComparison.Ordinal))
            {
                await sessionStore.UntrackAsync(
                    sessionId: session.SessionId,
                    cancellationToken: cancellationToken);
            }
        }

        private static void ApplyProvisioningStatus(
            ClassicCitySetupSessionState session,
            CityProvisioningStatusView provisioningStatus)
        {
            string bootstrapStatus = DetermineBootstrapStatus(provisioningStatus);
            CityPopulationBootstrapView? existingBootstrap = session.Provisioning?.PopulationBootstrap;
            DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;
            string simulationKind = session.SimulationKind ??
                                    session.LaunchRequest?.SimulationKind ??
                                    "ClassicCity";

            session.CityId = provisioningStatus.CityId;
            session.SimulationKind = simulationKind;
            session.Provisioning = new CityProvisioningView(
                CityId: provisioningStatus.CityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: new CityPopulationBootstrapView(
                    OperationId: provisioningStatus.PopulationBootstrapOperationId,
                    Status: bootstrapStatus,
                    PlannedPeopleCount: existingBootstrap?.PlannedPeopleCount ?? session.LaunchRequest?.PlannedPeopleCount,
                    ResidentialCapacity: existingBootstrap?.ResidentialCapacity,
                    Summary: existingBootstrap?.Summary,
                    FailureCode: string.Equals(bootstrapStatus, PopulationBootstrapStatusFailed, StringComparison.OrdinalIgnoreCase)
                        ? provisioningStatus.PopulationBootstrapFailureCode ?? existingBootstrap?.FailureCode
                        : null));
            session.UpdatedAtUtc = updatedAtUtc;

            if (string.Equals(bootstrapStatus, PopulationBootstrapStatusCompleted, StringComparison.OrdinalIgnoreCase))
            {
                session.Status = ClassicCitySetupSessionStatuses.Ready;
                session.FailureCode = null;
                session.FailureMessage = null;
                session.CompletedAtUtc = provisioningStatus.PopulationBootstrapCompletedAtUtc ?? updatedAtUtc;
                return;
            }

            if (string.Equals(bootstrapStatus, PopulationBootstrapStatusFailed, StringComparison.OrdinalIgnoreCase))
            {
                session.Status = ClassicCitySetupSessionStatuses.ProvisioningFailed;
                session.FailureCode =
                    provisioningStatus.PopulationBootstrapFailureCode ??
                    existingBootstrap?.FailureCode ??
                    ClassicCitySetupSessionFailureCodes.ProvisioningUnexpectedError;
                session.FailureMessage = "Population bootstrap failed and requires operator review.";
                session.CompletedAtUtc = provisioningStatus.PopulationBootstrapFailedAtUtc ?? updatedAtUtc;
                return;
            }

            session.Status = ClassicCitySetupSessionStatuses.BootstrappingPopulation;
            session.FailureCode = null;
            session.FailureMessage = null;
            session.CompletedAtUtc = null;
            session.StartedAtUtc ??= updatedAtUtc;
        }

        private static string DetermineBootstrapStatus(CityProvisioningStatusView provisioningStatus)
        {
            if (provisioningStatus.PopulationBootstrapFailedAtUtc.HasValue ||
                string.Equals(
                    provisioningStatus.Status,
                    "ProvisioningFailed",
                    StringComparison.OrdinalIgnoreCase))
            {
                return PopulationBootstrapStatusFailed;
            }

            if (provisioningStatus.PopulationBootstrapCompletedAtUtc.HasValue ||
                string.Equals(
                    provisioningStatus.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    provisioningStatus.Status,
                    "Archived",
                    StringComparison.OrdinalIgnoreCase))
            {
                return PopulationBootstrapStatusCompleted;
            }

            return PopulationBootstrapStatusPending;
        }

        private static bool ShouldStopTracking(string status)
        {
            return status is ClassicCitySetupSessionStatuses.Draft or
                ClassicCitySetupSessionStatuses.LaunchFailed or
                ClassicCitySetupSessionStatuses.Ready;
        }

        private bool ShouldRecoverQueuedLaunch(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    session.Status,
                    ClassicCitySetupSessionStatuses.LaunchQueued,
                    StringComparison.Ordinal))
            {
                return false;
            }

            DateTimeOffset queuedAtUtc = session.LaunchQueuedAtUtc ?? session.UpdatedAtUtc;
            TimeSpan age = DateTimeOffset.UtcNow - queuedAtUtc;

            return age >= TimeSpan.FromSeconds(_options.LaunchQueueRecoveryDelaySeconds);
        }

        private bool IsCreatingCityWithoutCorrelationStale(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    session.Status,
                    ClassicCitySetupSessionStatuses.CreatingCity,
                    StringComparison.Ordinal) ||
                session.CityId.HasValue)
            {
                return false;
            }

            DateTimeOffset referenceTimestamp = session.StartedAtUtc ?? session.UpdatedAtUtc;
            TimeSpan age = DateTimeOffset.UtcNow - referenceTimestamp;

            return age >= TimeSpan.FromSeconds(_options.LaunchQueueRecoveryDelaySeconds);
        }

        private bool ShouldRecoverCreatingCity(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    session.Status,
                    ClassicCitySetupSessionStatuses.CreatingCity,
                    StringComparison.Ordinal) ||
                session.CityId.HasValue ||
                session.LaunchRequest?.ProvisioningCorrelationId is null)
            {
                return false;
            }

            return IsCreatingCityWithoutCorrelationStale(session);
        }

        private static void FinalizeFromProvisioning(
            ClassicCitySetupSessionState session,
            CityProvisioningView provisioning)
        {
            string bootstrapStatus = provisioning.PopulationBootstrap.Status.Trim();

            session.CityId = provisioning.CityId;
            session.SimulationKind = provisioning.SimulationKind;
            session.Provisioning = provisioning;
            session.FailureCode = provisioning.PopulationBootstrap.FailureCode;
            session.FailureMessage = bootstrapStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                ? "Population bootstrap failed and requires operator review."
                : null;
            session.Status = bootstrapStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                ? ClassicCitySetupSessionStatuses.ProvisioningFailed
                : ClassicCitySetupSessionStatuses.Ready;
            session.CompletedAtUtc = DateTimeOffset.UtcNow;
            session.UpdatedAtUtc = session.CompletedAtUtc.Value;
        }

        private async Task FailLaunchAsync(
            ClassicCitySetupSessionState session,
            string failureCode,
            string failureMessage,
            CancellationToken cancellationToken)
        {
            DateTimeOffset completedAtUtc = DateTimeOffset.UtcNow;

            session.Status = ClassicCitySetupSessionStatuses.LaunchFailed;
            session.FailureCode = failureCode;
            session.FailureMessage = failureMessage;
            session.CompletedAtUtc = completedAtUtc;
            session.UpdatedAtUtc = completedAtUtc;

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);
        }

        private static string DetermineCityCreateFailureCode(Exception exception)
        {
            return exception switch
            {
                DownstreamServiceException downstreamException when
                    downstreamException.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                    ClassicCitySetupSessionFailureCodes.CityCreateValidationFailed,
                DownstreamServiceException downstreamException when downstreamException.StatusCode == HttpStatusCode.Conflict =>
                    ClassicCitySetupSessionFailureCodes.CityCreateConflict,
                DownstreamServiceException => ClassicCitySetupSessionFailureCodes.CityCreateUnexpectedError,
                HttpRequestException => ClassicCitySetupSessionFailureCodes.CityCreateTransportError,
                _ => ClassicCitySetupSessionFailureCodes.CityCreateUnexpectedError
            };
        }

        private static string BuildSafeFailureMessage(Exception exception, string fallback)
        {
            if (exception is DownstreamServiceException downstreamException &&
                !string.IsNullOrWhiteSpace(downstreamException.Message))
            {
                return FirstLineOrFallback(downstreamException.Message, fallback);
            }

            return FirstLineOrFallback(exception.Message, fallback);
        }

        private static string FirstLineOrFallback(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string firstLine = value
               .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .FirstOrDefault() ?? fallback;

            return firstLine.Length <= 220
                ? firstLine
                : firstLine[..220];
        }

        private static ClassicCitySetupSessionView MapToView(ClassicCitySetupSessionState session)
        {
            return new ClassicCitySetupSessionView(
                SessionId: session.SessionId,
                ScenarioKind: session.ScenarioKind,
                Status: session.Status,
                CurrentStepId: session.CurrentStepId,
                Draft: session.Draft,
                CityId: session.CityId,
                SimulationKind: session.SimulationKind,
                Provisioning: session.Provisioning,
                FailureCode: session.FailureCode,
                FailureMessage: session.FailureMessage,
                CreatedAtUtc: session.CreatedAtUtc,
                UpdatedAtUtc: session.UpdatedAtUtc,
                LaunchQueuedAtUtc: session.LaunchQueuedAtUtc,
                StartedAtUtc: session.StartedAtUtc,
                CompletedAtUtc: session.CompletedAtUtc);
        }

        private static ClassicCitySetupSessionMutationResult Updated(ClassicCitySetupSessionState session)
        {
            return new ClassicCitySetupSessionMutationResult(
                Status: ClassicCitySetupSessionMutationStatus.Updated,
                Session: MapToView(session),
                ErrorCode: null,
                ErrorMessage: null);
        }

        private static ClassicCitySetupSessionMutationResult Conflict(
            ClassicCitySetupSessionState session,
            string code,
            string message)
        {
            return new ClassicCitySetupSessionMutationResult(
                Status: ClassicCitySetupSessionMutationStatus.Conflict,
                Session: MapToView(session),
                ErrorCode: code,
                ErrorMessage: message);
        }

        private static ClassicCitySetupSessionMutationResult Unavailable(
            ClassicCitySetupSessionState session,
            string code,
            string message)
        {
            return new ClassicCitySetupSessionMutationResult(
                Status: ClassicCitySetupSessionMutationStatus.Unavailable,
                Session: MapToView(session),
                ErrorCode: code,
                ErrorMessage: message);
        }

        private static ClassicCitySetupSessionMutationResult Invalid(
            ClassicCitySetupSessionState session,
            string code,
            string message)
        {
            return new ClassicCitySetupSessionMutationResult(
                Status: ClassicCitySetupSessionMutationStatus.Invalid,
                Session: MapToView(session),
                ErrorCode: code,
                ErrorMessage: message);
        }

        private static ClassicCitySetupSessionMutationResult NotFound()
        {
            return new ClassicCitySetupSessionMutationResult(
                Status: ClassicCitySetupSessionMutationStatus.NotFound,
                Session: null,
                ErrorCode: null,
                ErrorMessage: null);
        }

        private async Task ExecuteWithSessionLockAsync(
            Guid sessionId,
            string? unavailableFallbackMessage,
            Func<ClassicCitySetupSessionState?, Task> action,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionLockHandle? lockHandle;

            try
            {
                lockHandle = await sessionStore.TryAcquireLockAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup session lock acquisition failed for sessionId={SessionId}.",
                    sessionId);

                if (string.IsNullOrWhiteSpace(unavailableFallbackMessage))
                    return;

                throw;
            }

            if (lockHandle is null)
            {
                if (string.IsNullOrWhiteSpace(unavailableFallbackMessage))
                {
                    logger.LogDebug(
                        message: "Classic City setup session is already locked for sessionId={SessionId}.",
                        sessionId);
                }

                return;
            }

            try
            {
                ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);

                await action(session);
            }
            finally
            {
                await TryReleaseSessionLockAsync(
                    sessionId: sessionId,
                    lockHandle: lockHandle,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task<ClassicCitySetupSessionMutationResult> ExecuteMutationAsync(
            Guid sessionId,
            string unavailableFallbackMessage,
            Func<Task<ClassicCitySetupSessionMutationResult>> action,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionLockHandle? lockHandle;

            try
            {
                lockHandle = await sessionStore.TryAcquireLockAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup session lock acquisition failed for sessionId={SessionId}.",
                    sessionId);

                ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);

                return session is null
                    ? NotFound()
                    : Unavailable(
                        session,
                        code: ClassicCitySetupSessionFailureCodes.SessionLockUnavailable,
                        message: unavailableFallbackMessage);
            }

            if (lockHandle is null)
            {
                ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);

                return session is null
                    ? NotFound()
                    : Conflict(
                        session,
                        code: ClassicCitySetupSessionFailureCodes.SessionBusy,
                        message: "This setup session is already being modified by another request. Retry in a moment.");
            }

            try
            {
                return await action();
            }
            finally
            {
                await TryReleaseSessionLockAsync(
                    sessionId: sessionId,
                    lockHandle: lockHandle,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task TryReleaseSessionLockAsync(
            Guid sessionId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken)
        {
            try
            {
                await sessionStore.ReleaseLockAsync(
                    sessionId: sessionId,
                    lockHandle: lockHandle,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup session lock release failed for sessionId={SessionId}.",
                    sessionId);
            }
        }
    }
}
