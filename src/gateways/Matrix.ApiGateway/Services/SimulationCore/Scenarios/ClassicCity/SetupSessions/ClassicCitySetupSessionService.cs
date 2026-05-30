using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MassTransit;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.Identity.Contracts.Internal.Responses;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionService(
        IClassicCitySetupSessionStore sessionStore,
        ICitiesApiClient citiesApiClient,
        ICityProvisioningService provisioningService,
        IPublishEndpoint publishEndpoint,
        IHttpContextAccessor httpContextAccessor,
        IPermissionsVersionStore permissionsVersionStore,
        IAuthContextStore authContextStore,
        IInternalJwtRequestContextAccessor internalJwtRequestContextAccessor,
        IOptions<ClassicCitySetupSessionOptions> options,
        TimeProvider timeProvider,
        ILogger<ClassicCitySetupSessionService> logger)
        : IClassicCitySetupSessionService
    {
        private const int MaxPlannedPeopleCount = 1_000_000;
        private const string EconomyProfileStruggling = "Struggling";
        private const string EconomyProfileBalanced = "Balanced";
        private const string EconomyProfileAffluent = "Affluent";
        private const string PopulationOccupancyProfileLight = "Light";
        private const string PopulationOccupancyProfileBalanced = "Balanced";
        private const string PopulationOccupancyProfileHigh = "High";
        private const string PopulationTargetModeRandom = "Random";
        private const string PopulationTargetModePreset1K = "Preset1K";
        private const string PopulationTargetModePreset10K = "Preset10K";
        private const string PopulationTargetModePreset100K = "Preset100K";
        private const string PopulationTargetModeManual = "Manual";
        private const string InitialWeatherModeRandom = "Random";
        private const string InitialWeatherModeManual = "Manual";
        private const int PopulationTargetPreset1K = 1_000;
        private const int PopulationTargetPreset10K = 10_000;
        private const int PopulationTargetPreset100K = 100_000;
        private const string PopulationBootstrapStatusPending = "Pending";
        private const string PopulationBootstrapStatusCompleted = "Completed";
        private const string PopulationBootstrapStatusFailed = "Failed";
        private const string EconomyBootstrapStatusPending = "Pending";
        private const string EconomyBootstrapStatusCompleted = "Completed";
        private const string EconomyBootstrapStatusFailed = "Failed";

        private static readonly string[] MutableStatuses =
        [
            ClassicCitySetupSessionStatuses.Draft,
            ClassicCitySetupSessionStatuses.LaunchFailed
        ];

        private readonly ClassicCitySetupSessionOptions _options = options.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<IReadOnlyList<ClassicCitySetupSessionView>> ListDraftsAsync(
            CancellationToken cancellationToken = default)
        {
            Guid ownerUserId = GetCurrentUserIdOrThrow();
            IReadOnlyList<ClassicCitySetupSessionState> sessions = await sessionStore.ListOwnedAsync(
                ownerUserId: ownerUserId,
                cancellationToken: cancellationToken);

            return sessions
               .Where(session => MutableStatuses.Contains(
                    value: session.Status,
                    comparer: StringComparer.Ordinal))
               .OrderByDescending(session => session.UpdatedAtUtc)
               .Select(MapToView)
               .ToArray();
        }

        public async Task<ClassicCitySetupSessionView> CreateAsync(
            CreateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Guid ownerUserId = GetCurrentUserIdOrThrow();
            string normalizedStepId = NormalizeStepId(request.CurrentStepId);
            string requestedReuseSignature = BuildDraftReuseSignature(
                currentStepId: normalizedStepId,
                draft: request.Draft);

            ClassicCitySetupSessionLockHandle? createLockHandle = null;

            try
            {
                try
                {
                    createLockHandle = await sessionStore.TryAcquireCreateLockAsync(
                        ownerUserId: ownerUserId,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException ||
                                           !cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Classic City setup session create-lock acquisition failed for ownerUserId={OwnerUserId}.",
                        ownerUserId);
                }

                IReadOnlyList<ClassicCitySetupSessionState> existingSessions = await sessionStore.ListOwnedAsync(
                    ownerUserId: ownerUserId,
                    cancellationToken: cancellationToken);

                ClassicCitySetupSessionState? reusableSession = FindReusableDraftCandidate(
                    sessions: existingSessions,
                    requestedReuseSignature: requestedReuseSignature,
                    now: now);

                if (reusableSession is not null)
                    return MapToView(reusableSession);

                var session = new ClassicCitySetupSessionState
                {
                    SessionId = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    ScenarioKind = "ClassicCity",
                    Status = ClassicCitySetupSessionStatuses.Draft,
                    CurrentStepId = normalizedStepId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                session.Draft = NormalizeDraft(
                    draft: request.Draft,
                    fallbackSeed: BuildDefaultGenerationSeed(session.SessionId));

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);

                return MapToView(session);
            }
            finally
            {
                await TryReleaseCreateLockAsync(
                    ownerUserId: ownerUserId,
                    createLockHandle: createLockHandle,
                    cancellationToken: cancellationToken);
            }
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
                : IsOwnedByCurrentUser(session)
                    ? MapToView(session)
                    : null;
        }

        public async Task<ClassicCitySetupSessionMutationResult> DeleteAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteMutationAsync(
                sessionId: sessionId,
                unavailableFallbackMessage: "Setup session is temporarily unavailable for deletion.",
                action: async () =>
                {
                    ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                        sessionId: sessionId,
                        cancellationToken: cancellationToken);

                    if (session is null)
                        return NotFound();

                    if (!TryAttachOrValidateOwner(session))
                        return NotFound();

                    if (!MutableStatuses.Contains(
                            value: session.Status,
                            comparer: StringComparer.Ordinal))
                        return Conflict(
                            session: session,
                            code: ClassicCitySetupSessionFailureCodes.InvalidLaunchState,
                            message: "Only draft setup sessions can be deleted.");

                    await sessionStore.DeleteAsync(
                        sessionId: session.SessionId,
                        ownerUserId: session.OwnerUserId,
                        cancellationToken: cancellationToken);

                    return new ClassicCitySetupSessionMutationResult(
                        Status: ClassicCitySetupSessionMutationStatus.Updated,
                        Session: null,
                        ErrorCode: null,
                        ErrorMessage: null);
                },
                cancellationToken: cancellationToken);
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

                    if (!TryAttachOrValidateOwner(session))
                        return NotFound();

                    if (!MutableStatuses.Contains(
                            value: session.Status,
                            comparer: StringComparer.Ordinal))
                        return Conflict(
                            session: session,
                            code: ClassicCitySetupSessionFailureCodes.InvalidLaunchState,
                            message:
                            "This setup session can no longer be edited because launch orchestration is already in progress or completed.");

                    session.CurrentStepId = NormalizeStepId(request.CurrentStepId);
                    session.Draft = NormalizeDraft(
                        draft: request.Draft,
                        fallbackSeed: ResolveGenerationSeedFallback(session));
                    session.UpdatedAtUtc = _timeProvider.GetUtcNow();

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

                    if (!TryAttachOrValidateOwner(session))
                        return NotFound();

                    if (!MutableStatuses.Contains(
                            value: session.Status,
                            comparer: StringComparer.Ordinal))
                        return Conflict(
                            session: session,
                            code: ClassicCitySetupSessionFailureCodes.InvalidLaunchState,
                            message: "This setup session is already queued, running, or attached to a launched city.");

                    ClassicCitySetupDraftDto draft = NormalizeDraft(
                        draft: session.Draft,
                        fallbackSeed: ResolveGenerationSeedFallback(session));

                    if (!TryBuildLaunchRequest(
                            draft: draft,
                            launchRequest: out CreateCityRequestDto? launchRequest,
                            errorMessage: out string? errorMessage))
                    {
                        session.Draft = draft;
                        session.UpdatedAtUtc = _timeProvider.GetUtcNow();

                        await sessionStore.SaveAsync(
                            session: session,
                            cancellationToken: cancellationToken);

                        return Invalid(
                            session: session,
                            code: "Gateway.ClassicCitySetup.ValidationFailed",
                            message: errorMessage ?? "Setup draft is incomplete.");
                    }

                    launchRequest = launchRequest! with
                    {
                        ProvisioningCorrelationId = session.SessionId
                    };

                    ClassicCitySetupSessionLaunchAuthSnapshot launchAuthContext;
                    try
                    {
                        launchAuthContext = await CaptureLaunchAuthSnapshotAsync(cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException ||
                                               !cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            exception: ex,
                            message:
                            "Classic City setup launch auth context could not be captured for sessionId={SessionId}.",
                            session.SessionId);

                        return Unavailable(
                            session: session,
                            code: ClassicCitySetupSessionFailureCodes.LaunchAuthContextUnavailable,
                            message:
                            "Launch auth context could not be captured. Retry when gateway identity context is healthy.");
                    }

                    DateTimeOffset now = _timeProvider.GetUtcNow();
                    session.Status = ClassicCitySetupSessionStatuses.LaunchQueued;
                    session.CurrentStepId = ClassicCitySetupSteps.Launch;
                    session.Draft = draft;
                    session.LaunchRequest = launchRequest;
                    session.LaunchAuthContext = launchAuthContext;
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
                    catch (Exception ex) when (ex is not OperationCanceledException ||
                                               !cancellationToken.IsCancellationRequested)
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
                                fallback:
                                "Launch request could not be queued. Retry when gateway messaging is healthy."),
                            cancellationToken: cancellationToken);

                        return Unavailable(
                            session: session,
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
                            a: session.Status,
                            b: ClassicCitySetupSessionStatuses.LaunchQueued,
                            comparisonType: StringComparison.Ordinal))
                        return;

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
                            message:
                            "Classic City setup session recovery is resuming a stale queued launch for sessionId={SessionId}.",
                            session.SessionId);

                        await ProcessLaunchCoreAsync(
                            session: session,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (ShouldRecoverCreatingCity(session))
                    {
                        logger.LogInformation(
                            message:
                            "Classic City setup session recovery is replaying city creation for stale sessionId={SessionId}.",
                            session.SessionId);

                        await ProcessLaunchCoreAsync(
                            session: session,
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (!session.CityId.HasValue)
                    {
                        if (IsCreatingCityWithoutCorrelationStale(session))
                            logger.LogWarning(
                                message:
                                "Classic City setup session reconciliation detected a stale creating-city state without a city correlation for sessionId={SessionId}. Manual review may be required.",
                                session.SessionId);

                        return;
                    }

                    await ReconcileProvisioningAsync(
                        session: session,
                        cancellationToken: cancellationToken);
                },
                cancellationToken: cancellationToken);
        }

        private static ClassicCitySetupDraftDto NormalizeDraft(
            ClassicCitySetupDraftDto draft,
            string fallbackSeed)
        {
            ArgumentNullException.ThrowIfNull(draft);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackSeed);

            return draft with
            {
                Name = draft.Name?.Trim() ?? string.Empty,
                StartSimTimeLocal = draft.StartSimTimeLocal?.Trim() ?? string.Empty,
                SpeedMultiplier = draft.SpeedMultiplier?.Trim() ?? string.Empty,
                ClimateZone = string.IsNullOrWhiteSpace(draft.ClimateZone)
                    ? "Temperate"
                    : draft.ClimateZone.Trim(),
                Hemisphere = string.IsNullOrWhiteSpace(draft.Hemisphere)
                    ? "Northern"
                    : draft.Hemisphere.Trim(),
                UtcOffsetMinutes = draft.UtcOffsetMinutes?.Trim() ?? string.Empty,
                GenerationSeed = string.IsNullOrWhiteSpace(draft.GenerationSeed)
                    ? fallbackSeed
                    : draft.GenerationSeed.Trim(),
                InitialWeatherMode = NormalizeInitialWeatherMode(draft.InitialWeatherMode),
                InitialWeatherType = NormalizeInitialWeatherType(draft.InitialWeatherType),
                InitialWeatherSeverity = NormalizeInitialWeatherSeverity(draft.InitialWeatherSeverity),
                InitialWeatherTemperatureC = draft.InitialWeatherTemperatureC?.Trim() ?? string.Empty,
                PopulationTargetMode = NormalizePopulationTargetMode(
                    value: draft.PopulationTargetMode,
                    plannedPeopleCount: draft.PlannedPeopleCount,
                    sizeTier: draft.SizeTier),
                SizeTier = string.IsNullOrWhiteSpace(draft.SizeTier)
                    ? "Medium"
                    : draft.SizeTier.Trim(),
                UrbanDensity = string.IsNullOrWhiteSpace(draft.UrbanDensity)
                    ? "Balanced"
                    : draft.UrbanDensity.Trim(),
                DevelopmentLevel = string.IsNullOrWhiteSpace(draft.DevelopmentLevel)
                    ? "Balanced"
                    : draft.DevelopmentLevel.Trim(),
                EconomyProfile = NormalizeEconomyProfile(draft.EconomyProfile),
                PopulationOccupancyProfile = NormalizePopulationOccupancyProfile(draft.PopulationOccupancyProfile),
                PlannedPeopleCount = draft.PlannedPeopleCount?.Trim() ?? string.Empty
            };
        }

        private static string ResolveGenerationSeedFallback(ClassicCitySetupSessionState session)
        {
            return !string.IsNullOrWhiteSpace(session.Draft.GenerationSeed)
                ? session.Draft.GenerationSeed
                : BuildDefaultGenerationSeed(session.SessionId);
        }

        private static string BuildDefaultGenerationSeed(Guid sessionId)
        {
            string compact = sessionId.ToString("N")[..16];
            return $"cc-{compact}";
        }

        private static string NormalizeStepId(string? currentStepId)
        {
            string normalized = currentStepId?.Trim()
                                   .ToLowerInvariant() ??
                                ClassicCitySetupSteps.Scenario;
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
                    style: NumberStyles.Number,
                    provider: CultureInfo.InvariantCulture,
                    result: out decimal speedMultiplier) ||
                speedMultiplier <= 0)
            {
                errorMessage = "Speed multiplier must be a number greater than 0.";
                return false;
            }

            if (!int.TryParse(
                    s: draft.UtcOffsetMinutes,
                    style: NumberStyles.Integer,
                    provider: CultureInfo.InvariantCulture,
                    result: out int utcOffsetMinutes) ||
                utcOffsetMinutes < -14 * 60 ||
                utcOffsetMinutes > 14 * 60)
            {
                errorMessage = "UTC offset must stay between -840 and 840 minutes.";
                return false;
            }

            int? plannedPeopleCount = null;
            decimal? initialWeatherTemperatureC = null;

            if (!TryResolvePlannedPeopleCount(
                    draft: draft,
                    plannedPeopleCount: out plannedPeopleCount,
                    errorMessage: out errorMessage))
                return false;

            if (!TryResolveInitialWeatherTemperature(
                    draft: draft,
                    initialWeatherTemperatureC: out initialWeatherTemperatureC,
                    errorMessage: out errorMessage))
                return false;

            launchRequest = new CreateCityRequestDto(
                Name: draft.Name,
                StartSimTimeUtc: draft.StartSimTimeUtc.Value,
                SpeedMultiplier: speedMultiplier,
                ClimateZone: draft.ClimateZone,
                Hemisphere: draft.Hemisphere,
                UtcOffsetMinutes: utcOffsetMinutes,
                GenerationSeed: string.IsNullOrWhiteSpace(draft.GenerationSeed)
                    ? null
                    : draft.GenerationSeed,
                SizeTier: draft.SizeTier,
                UrbanDensity: draft.UrbanDensity,
                DevelopmentLevel: draft.DevelopmentLevel,
                EconomyProfile: draft.EconomyProfile,
                PopulationOccupancyProfile: draft.PopulationOccupancyProfile,
                InitialWeatherMode: draft.InitialWeatherMode,
                InitialWeatherType: draft.InitialWeatherType,
                InitialWeatherSeverity: draft.InitialWeatherSeverity,
                InitialWeatherTemperatureC: initialWeatherTemperatureC,
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

        private static string NormalizeEconomyProfile(string? value)
        {
            return value?.Trim() switch
            {
                EconomyProfileStruggling => EconomyProfileStruggling,
                EconomyProfileAffluent => EconomyProfileAffluent,
                _ => EconomyProfileBalanced
            };
        }

        private static string NormalizeInitialWeatherMode(string? value)
        {
            return value?.Trim() switch
            {
                InitialWeatherModeManual => InitialWeatherModeManual,
                _ => InitialWeatherModeRandom
            };
        }

        private static string NormalizeInitialWeatherType(string? value)
        {
            return value?.Trim() switch
            {
                "Overcast" => "Overcast",
                "Rain" => "Rain",
                "Snow" => "Snow",
                "Storm" => "Storm",
                "Fog" => "Fog",
                "Windy" => "Windy",
                "Heatwave" => "Heatwave",
                "ColdSnap" => "ColdSnap",
                _ => "Clear"
            };
        }

        private static string NormalizeInitialWeatherSeverity(string? value)
        {
            return value?.Trim() switch
            {
                "Calm" => "Calm",
                "Moderate" => "Moderate",
                "Severe" => "Severe",
                "Extreme" => "Extreme",
                _ => "Mild"
            };
        }

        private static string NormalizePopulationTargetMode(
            string? value,
            string? plannedPeopleCount,
            string? sizeTier)
        {
            return value?.Trim() switch
            {
                PopulationTargetModeRandom => PopulationTargetModeRandom,
                PopulationTargetModePreset1K => PopulationTargetModePreset1K,
                PopulationTargetModePreset10K => PopulationTargetModePreset10K,
                PopulationTargetModePreset100K => PopulationTargetModePreset100K,
                PopulationTargetModeManual => PopulationTargetModeManual,
                _ when !string.IsNullOrWhiteSpace(plannedPeopleCount) => PopulationTargetModeManual,
                _ => sizeTier?.Trim() switch
                {
                    "Small" => PopulationTargetModePreset1K,
                    "Large" => PopulationTargetModePreset100K,
                    _ => PopulationTargetModePreset10K
                }
            };
        }

        private static bool TryResolvePlannedPeopleCount(
            ClassicCitySetupDraftDto draft,
            out int? plannedPeopleCount,
            out string? errorMessage)
        {
            plannedPeopleCount = draft.PopulationTargetMode switch
            {
                PopulationTargetModePreset1K => PopulationTargetPreset1K,
                PopulationTargetModePreset10K => PopulationTargetPreset10K,
                PopulationTargetModePreset100K => PopulationTargetPreset100K,
                PopulationTargetModeRandom => ResolveDeterministicRandomTarget(draft.GenerationSeed),
                _ => null
            };
            errorMessage = null;

            if (string.Equals(
                    a: draft.PopulationTargetMode,
                    b: PopulationTargetModeManual,
                    comparisonType: StringComparison.Ordinal))
            {
                if (!int.TryParse(
                        s: draft.PlannedPeopleCount,
                        style: NumberStyles.Integer,
                        provider: CultureInfo.InvariantCulture,
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

            return true;
        }

        private static bool TryResolveInitialWeatherTemperature(
            ClassicCitySetupDraftDto draft,
            out decimal? initialWeatherTemperatureC,
            out string? errorMessage)
        {
            initialWeatherTemperatureC = null;
            errorMessage = null;

            if (!string.Equals(
                    a: draft.InitialWeatherMode,
                    b: InitialWeatherModeManual,
                    comparisonType: StringComparison.Ordinal))
                return true;

            if (string.IsNullOrWhiteSpace(draft.InitialWeatherType))
            {
                errorMessage = "Initial weather type is required for manual weather mode.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(draft.InitialWeatherSeverity))
            {
                errorMessage = "Initial weather severity is required for manual weather mode.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(draft.InitialWeatherTemperatureC))
                return true;

            if (!decimal.TryParse(
                    s: draft.InitialWeatherTemperatureC,
                    style: NumberStyles.Number,
                    provider: CultureInfo.InvariantCulture,
                    result: out decimal parsedTemperature))
            {
                errorMessage = "Initial weather temperature must be a valid number.";
                return false;
            }

            if (parsedTemperature is < -100m or > 80m)
            {
                errorMessage = "Initial weather temperature must stay between -100 and 80 Celsius.";
                return false;
            }

            initialWeatherTemperatureC = parsedTemperature;
            return true;
        }

        private static int ResolveDeterministicRandomTarget(string generationSeed)
        {
            uint hash = ComputeFnv1A32($"{generationSeed.Trim()}|classic-city|population-target");
            int[] anchors =
            [
                PopulationTargetPreset1K,
                PopulationTargetPreset10K,
                PopulationTargetPreset100K
            ];
            int anchor = anchors[(int)(hash % (uint)anchors.Length)];
            int jitterBasis = (int)(hash / (uint)anchors.Length % 41U);
            decimal jitterPercent = (jitterBasis - 20) / 100.0m;
            int rawTarget = Math.Max(
                val1: 100,
                val2: (int)Math.Round(
                    d: anchor * (1.0m + jitterPercent),
                    mode: MidpointRounding.AwayFromZero));

            if (rawTarget >= PopulationTargetPreset100K)
                return RoundToStep(
                    value: rawTarget,
                    step: 1_000);

            if (rawTarget >= PopulationTargetPreset10K)
                return RoundToStep(
                    value: rawTarget,
                    step: 100);

            return RoundToStep(
                value: rawTarget,
                step: 10);
        }

        private static uint ComputeFnv1A32(string value)
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            uint hash = offsetBasis;

            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash;
        }

        private static int RoundToStep(
            int value,
            int step)
        {
            return Math.Max(
                       val1: step,
                       val2: (int)Math.Round(
                           d: value / (decimal)step,
                           mode: MidpointRounding.AwayFromZero)) *
                   step;
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

            DateTimeOffset startedAtUtc = _timeProvider.GetUtcNow();
            session.Status = ClassicCitySetupSessionStatuses.CreatingCity;
            session.StartedAtUtc ??= startedAtUtc;
            session.UpdatedAtUtc = startedAtUtc;

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            try
            {
                CityProvisioningView provisioning = await ExecuteWithLaunchAuthAsync(
                    session: session,
                    cancellationToken: cancellationToken,
                    action: () => provisioningService.CreateCityAsync(
                        request: session.LaunchRequest,
                        cancellationToken: cancellationToken));

                FinalizeFromProvisioning(
                    session: session,
                    provisioning: provisioning,
                    completedAtUtc: _timeProvider.GetUtcNow());

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException ||
                                       !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup launch failed during provisioning for sessionId={SessionId}.",
                    session.SessionId);

                await FailLaunchAsync(
                    session: session,
                    failureCode: DetermineCityCreateFailureCode(ex),
                    failureMessage: BuildSafeFailureMessage(
                        exception: ex,
                        fallback: "City launch failed before provisioning could complete."),
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
                provisioningStatus = await ExecuteWithLaunchAuthAsync(
                    session: session,
                    cancellationToken: cancellationToken,
                    action: () => citiesApiClient.GetProvisioningStatusAsync(
                        cityId: session.CityId!.Value,
                        cancellationToken: cancellationToken));
            }
            catch (DownstreamServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                session.Status = ClassicCitySetupSessionStatuses.ProvisioningFailed;
                session.FailureCode = ClassicCitySetupSessionFailureCodes.ReconciliationCityNotFound;
                session.FailureMessage =
                    "Gateway could not find the city host during reconciliation. Operator review is required.";
                session.CompletedAtUtc = _timeProvider.GetUtcNow();
                session.UpdatedAtUtc = session.CompletedAtUtc.Value;

                await sessionStore.SaveAsync(
                    session: session,
                    cancellationToken: cancellationToken);
                return;
            }

            ApplyProvisioningStatus(
                session: session,
                provisioningStatus: provisioningStatus,
                updatedAtUtc: _timeProvider.GetUtcNow());

            await sessionStore.SaveAsync(
                session: session,
                cancellationToken: cancellationToken);

            if (string.Equals(
                    a: session.Status,
                    b: ClassicCitySetupSessionStatuses.Ready,
                    comparisonType: StringComparison.Ordinal))
                await sessionStore.UntrackAsync(
                    sessionId: session.SessionId,
                    cancellationToken: cancellationToken);
        }

        private static void ApplyProvisioningStatus(
            ClassicCitySetupSessionState session,
            CityProvisioningStatusView provisioningStatus,
            DateTimeOffset updatedAtUtc)
        {
            string populationBootstrapStatus = DeterminePopulationBootstrapStatus(provisioningStatus);
            string economyBootstrapStatus = DetermineEconomyBootstrapStatus(provisioningStatus);
            CityPopulationBootstrapView? existingBootstrap = session.Provisioning?.PopulationBootstrap;
            CityEconomyBootstrapView? existingEconomyBootstrap = session.Provisioning?.EconomyBootstrap;
            string simulationKind = session.SimulationKind ?? "ClassicCity";

            session.CityId = provisioningStatus.CityId;
            session.SimulationKind = simulationKind;
            session.Provisioning = new CityProvisioningView(
                CityId: provisioningStatus.CityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: new CityPopulationBootstrapView(
                    OperationId: provisioningStatus.PopulationBootstrapOperationId,
                    Status: populationBootstrapStatus,
                    PlannedPeopleCount: existingBootstrap?.PlannedPeopleCount ??
                                        session.LaunchRequest?.PlannedPeopleCount,
                    ResidentialCapacity: existingBootstrap?.ResidentialCapacity,
                    Summary: existingBootstrap?.Summary,
                    FailureCode: string.Equals(
                        a: populationBootstrapStatus,
                        b: PopulationBootstrapStatusFailed,
                        comparisonType: StringComparison.OrdinalIgnoreCase)
                        ? provisioningStatus.PopulationBootstrapFailureCode ?? existingBootstrap?.FailureCode
                        : null),
                EconomyBootstrap: new CityEconomyBootstrapView(
                    OperationId: provisioningStatus.EconomyBootstrapOperationId,
                    Status: economyBootstrapStatus,
                    FailureCode: string.Equals(
                        a: economyBootstrapStatus,
                        b: EconomyBootstrapStatusFailed,
                        comparisonType: StringComparison.OrdinalIgnoreCase)
                        ? provisioningStatus.EconomyBootstrapFailureCode ?? existingEconomyBootstrap?.FailureCode
                        : null,
                    UnitKind: existingEconomyBootstrap?.UnitKind,
                    UnitCode: existingEconomyBootstrap?.UnitCode,
                    UnitDisplayName: existingEconomyBootstrap?.UnitDisplayName,
                    UnitSymbol: existingEconomyBootstrap?.UnitSymbol));
            session.UpdatedAtUtc = updatedAtUtc;

            if (string.Equals(
                    a: populationBootstrapStatus,
                    b: PopulationBootstrapStatusCompleted,
                    comparisonType: StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    a: economyBootstrapStatus,
                    b: EconomyBootstrapStatusCompleted,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                session.Status = ClassicCitySetupSessionStatuses.Ready;
                session.FailureCode = null;
                session.FailureMessage = null;
                session.CompletedAtUtc =
                    provisioningStatus.EconomyBootstrapCompletedAtUtc ??
                    provisioningStatus.PopulationBootstrapCompletedAtUtc ??
                    updatedAtUtc;
                return;
            }

            if (string.Equals(
                    a: populationBootstrapStatus,
                    b: PopulationBootstrapStatusFailed,
                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    a: economyBootstrapStatus,
                    b: EconomyBootstrapStatusFailed,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                session.Status = ClassicCitySetupSessionStatuses.ProvisioningFailed;
                bool economyFailed = string.Equals(
                    a: economyBootstrapStatus,
                    b: EconomyBootstrapStatusFailed,
                    comparisonType: StringComparison.OrdinalIgnoreCase);
                session.FailureCode = economyFailed
                    ? provisioningStatus.EconomyBootstrapFailureCode ??
                      existingEconomyBootstrap?.FailureCode ??
                      ClassicCitySetupSessionFailureCodes.ProvisioningUnexpectedError
                    : provisioningStatus.PopulationBootstrapFailureCode ??
                      existingBootstrap?.FailureCode ??
                      ClassicCitySetupSessionFailureCodes.ProvisioningUnexpectedError;
                session.FailureMessage = economyFailed
                    ? "Economy bootstrap failed and requires operator review."
                    : "Population bootstrap failed and requires operator review.";
                session.CompletedAtUtc = economyFailed
                    ? provisioningStatus.EconomyBootstrapFailedAtUtc ?? updatedAtUtc
                    : provisioningStatus.PopulationBootstrapFailedAtUtc ?? updatedAtUtc;
                return;
            }

            session.Status = ClassicCitySetupSessionStatuses.BootstrappingPopulation;
            session.FailureCode = null;
            session.FailureMessage = null;
            session.CompletedAtUtc = null;
            session.StartedAtUtc ??= updatedAtUtc;
        }

        private static string DeterminePopulationBootstrapStatus(CityProvisioningStatusView provisioningStatus)
        {
            if (provisioningStatus.PopulationBootstrapFailedAtUtc.HasValue ||
                string.Equals(
                    a: provisioningStatus.Status,
                    b: "ProvisioningFailed",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return PopulationBootstrapStatusFailed;

            if (provisioningStatus.PopulationBootstrapCompletedAtUtc.HasValue ||
                string.Equals(
                    a: provisioningStatus.Status,
                    b: "Active",
                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    a: provisioningStatus.Status,
                    b: "Archived",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return PopulationBootstrapStatusCompleted;

            return PopulationBootstrapStatusPending;
        }

        private static string DetermineEconomyBootstrapStatus(CityProvisioningStatusView provisioningStatus)
        {
            if (provisioningStatus.EconomyBootstrapFailedAtUtc.HasValue ||
                (string.Equals(
                     a: provisioningStatus.Status,
                     b: "ProvisioningFailed",
                     comparisonType: StringComparison.OrdinalIgnoreCase) &&
                 !string.IsNullOrWhiteSpace(provisioningStatus.EconomyBootstrapFailureCode)))
                return EconomyBootstrapStatusFailed;

            if (provisioningStatus.EconomyBootstrapCompletedAtUtc.HasValue ||
                string.Equals(
                    a: provisioningStatus.Status,
                    b: "Active",
                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    a: provisioningStatus.Status,
                    b: "Archived",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return EconomyBootstrapStatusCompleted;

            return EconomyBootstrapStatusPending;
        }

        private static bool ShouldStopTracking(string status)
        {
            return status is ClassicCitySetupSessionStatuses.Draft
             or ClassicCitySetupSessionStatuses.LaunchFailed
             or ClassicCitySetupSessionStatuses.Ready;
        }

        private bool ShouldRecoverQueuedLaunch(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    a: session.Status,
                    b: ClassicCitySetupSessionStatuses.LaunchQueued,
                    comparisonType: StringComparison.Ordinal))
                return false;

            DateTimeOffset queuedAtUtc = session.LaunchQueuedAtUtc ?? session.UpdatedAtUtc;
            TimeSpan age = _timeProvider.GetUtcNow() - queuedAtUtc;

            return age >= TimeSpan.FromSeconds(_options.LaunchQueueRecoveryDelaySeconds);
        }

        private bool IsCreatingCityWithoutCorrelationStale(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    a: session.Status,
                    b: ClassicCitySetupSessionStatuses.CreatingCity,
                    comparisonType: StringComparison.Ordinal) ||
                session.CityId.HasValue)
                return false;

            DateTimeOffset referenceTimestamp = session.StartedAtUtc ?? session.UpdatedAtUtc;
            TimeSpan age = _timeProvider.GetUtcNow() - referenceTimestamp;

            return age >= TimeSpan.FromSeconds(_options.LaunchQueueRecoveryDelaySeconds);
        }

        private bool ShouldRecoverCreatingCity(ClassicCitySetupSessionState session)
        {
            if (!string.Equals(
                    a: session.Status,
                    b: ClassicCitySetupSessionStatuses.CreatingCity,
                    comparisonType: StringComparison.Ordinal) ||
                session.CityId.HasValue ||
                session.LaunchRequest?.ProvisioningCorrelationId is null)
                return false;

            return IsCreatingCityWithoutCorrelationStale(session);
        }

        private static void FinalizeFromProvisioning(
            ClassicCitySetupSessionState session,
            CityProvisioningView provisioning,
            DateTimeOffset completedAtUtc)
        {
            string populationBootstrapStatus = provisioning.PopulationBootstrap.Status.Trim();
            string economyBootstrapStatus = provisioning.EconomyBootstrap.Status.Trim();

            session.CityId = provisioning.CityId;
            session.SimulationKind = provisioning.SimulationKind;
            session.Provisioning = provisioning;
            bool economyFailed = economyBootstrapStatus.Equals(
                value: EconomyBootstrapStatusFailed,
                comparisonType: StringComparison.OrdinalIgnoreCase);
            bool populationFailed = populationBootstrapStatus.Equals(
                value: PopulationBootstrapStatusFailed,
                comparisonType: StringComparison.OrdinalIgnoreCase);

            session.FailureCode = economyFailed
                ? provisioning.EconomyBootstrap.FailureCode
                : provisioning.PopulationBootstrap.FailureCode;
            session.FailureMessage = economyFailed
                ? "Economy bootstrap failed and requires operator review."
                : populationFailed
                    ? "Population bootstrap failed and requires operator review."
                    : null;
            session.Status = economyFailed || populationFailed
                ? ClassicCitySetupSessionStatuses.ProvisioningFailed
                : ClassicCitySetupSessionStatuses.Ready;
            session.CompletedAtUtc = completedAtUtc;
            session.UpdatedAtUtc = session.CompletedAtUtc.Value;
        }

        private async Task FailLaunchAsync(
            ClassicCitySetupSessionState session,
            string failureCode,
            string failureMessage,
            CancellationToken cancellationToken)
        {
            DateTimeOffset completedAtUtc = _timeProvider.GetUtcNow();

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
                DownstreamServiceException downstreamException when downstreamException.StatusCode ==
                                                                    HttpStatusCode.Conflict =>
                    ClassicCitySetupSessionFailureCodes.CityCreateConflict,
                DownstreamServiceException => ClassicCitySetupSessionFailureCodes.CityCreateUnexpectedError,
                HttpRequestException => ClassicCitySetupSessionFailureCodes.CityCreateTransportError,
                _ => ClassicCitySetupSessionFailureCodes.CityCreateUnexpectedError
            };
        }

        private static string BuildSafeFailureMessage(
            Exception exception,
            string fallback)
        {
            if (exception is DownstreamServiceException downstreamException &&
                !string.IsNullOrWhiteSpace(downstreamException.Message))
                return FirstLineOrFallback(
                    value: downstreamException.Message,
                    fallback: fallback);

            return FirstLineOrFallback(
                value: exception.Message,
                fallback: fallback);
        }

        private static string FirstLineOrFallback(
            string? value,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string firstLine = value
                                  .Split(
                                       separator: '\n',
                                       options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                  .FirstOrDefault() ??
                               fallback;

            return firstLine.Length <= 220
                ? firstLine
                : firstLine[..220];
        }

        private bool IsOwnedByCurrentUser(ClassicCitySetupSessionState session)
        {
            Guid? currentUserId = TryGetCurrentUserId();
            return currentUserId is not null &&
                   (session.OwnerUserId is null || session.OwnerUserId == currentUserId);
        }

        private bool TryAttachOrValidateOwner(ClassicCitySetupSessionState session)
        {
            Guid currentUserId = GetCurrentUserIdOrThrow();

            if (session.OwnerUserId is null)
            {
                session.OwnerUserId = currentUserId;
                return true;
            }

            return session.OwnerUserId == currentUserId;
        }

        private Guid GetCurrentUserIdOrThrow()
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            string? sub =
                user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(
                    input: sub,
                    result: out Guid userId))
                return userId;

            throw new InvalidOperationException(
                "A current authenticated gateway user is required for setup session mutation.");
        }

        private Guid? TryGetCurrentUserId()
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            string? sub =
                user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(
                input: sub,
                result: out Guid userId)
                ? userId
                : null;
        }

        private async Task<ClassicCitySetupSessionLaunchAuthSnapshot> CaptureLaunchAuthSnapshotAsync(
            CancellationToken cancellationToken)
        {
            Guid userId = GetCurrentUserIdOrThrow();
            string? jti = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Jti);

            return await BuildLaunchAuthSnapshotAsync(
                userId: userId,
                jti: jti,
                cancellationToken: cancellationToken);
        }

        private async Task<TResult> ExecuteWithLaunchAuthAsync<TResult>(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken,
            Func<Task<TResult>> action)
        {
            InternalJwtRequestContext requestContext = await ResolveLaunchRequestContextAsync(
                session: session,
                cancellationToken: cancellationToken);

            using IDisposable _ = internalJwtRequestContextAccessor.Push(requestContext);
            return await action();
        }

        private async Task<InternalJwtRequestContext> ResolveLaunchRequestContextAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionLaunchAuthSnapshot? snapshot = session.LaunchAuthContext;

            if (snapshot is null)
            {
                if (session.OwnerUserId is not Guid ownerUserId)
                    throw new InvalidOperationException("Setup session launch auth context is missing.");

                snapshot = await BuildLaunchAuthSnapshotAsync(
                    userId: ownerUserId,
                    jti: null,
                    cancellationToken: cancellationToken);

                session.LaunchAuthContext = snapshot;
            }

            return new InternalJwtRequestContext(
                UserId: snapshot.UserId,
                Jti: snapshot.Jti,
                PermissionsVersion: snapshot.PermissionsVersion,
                EffectivePermissions: snapshot.EffectivePermissions);
        }

        private async Task<ClassicCitySetupSessionLaunchAuthSnapshot> BuildLaunchAuthSnapshotAsync(
            Guid userId,
            string? jti,
            CancellationToken cancellationToken)
        {
            int currentPermissionsVersion = await permissionsVersionStore.GetCurrentAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            UserAuthContextResponse authContext = await authContextStore.GetAsync(
                userId: userId,
                permissionsVersion: currentPermissionsVersion,
                ct: cancellationToken);

            return new ClassicCitySetupSessionLaunchAuthSnapshot
            {
                UserId = userId,
                Jti = string.IsNullOrWhiteSpace(jti)
                    ? null
                    : jti.Trim(),
                PermissionsVersion = authContext.PermissionsVersion,
                EffectivePermissions = authContext.EffectivePermissions
                   .Where(permission => !string.IsNullOrWhiteSpace(permission))
                   .Distinct(StringComparer.Ordinal)
                   .ToArray(),
                CapturedAtUtc = _timeProvider.GetUtcNow()
            };
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

        private ClassicCitySetupSessionState? FindReusableDraftCandidate(
            IReadOnlyList<ClassicCitySetupSessionState> sessions,
            string requestedReuseSignature,
            DateTimeOffset now)
        {
            var reuseWindow = TimeSpan.FromSeconds(_options.RecentDraftReuseWindowSeconds);

            return sessions.FirstOrDefault(session =>
                string.Equals(
                    a: session.Status,
                    b: ClassicCitySetupSessionStatuses.Draft,
                    comparisonType: StringComparison.Ordinal) &&
                now - session.UpdatedAtUtc <= reuseWindow &&
                string.Equals(
                    a: BuildDraftReuseSignature(
                        currentStepId: session.CurrentStepId,
                        draft: session.Draft),
                    b: requestedReuseSignature,
                    comparisonType: StringComparison.Ordinal));
        }

        private static string BuildDraftReuseSignature(
            string currentStepId,
            ClassicCitySetupDraftDto draft)
        {
            ClassicCitySetupDraftDto normalizedDraft = NormalizeDraft(
                draft: draft,
                fallbackSeed: "reuse-seed");

            ClassicCitySetupDraftDto reuseComparableDraft = normalizedDraft with
            {
                StartSimTimeLocal = string.Empty,
                StartSimTimeUtc = null,
                GenerationSeed = string.Empty
            };

            return JsonSerializer.Serialize(
                new
                {
                    CurrentStepId = NormalizeStepId(currentStepId),
                    Draft = reuseComparableDraft
                });
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
            catch (Exception ex) when (ex is not OperationCanceledException ||
                                       !cancellationToken.IsCancellationRequested)
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
                    logger.LogDebug(
                        message: "Classic City setup session is already locked for sessionId={SessionId}.",
                        sessionId);

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
            catch (Exception ex) when (ex is not OperationCanceledException ||
                                       !cancellationToken.IsCancellationRequested)
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
                        session: session,
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
                        session: session,
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
            catch (Exception ex) when (ex is not OperationCanceledException ||
                                       !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup session lock release failed for sessionId={SessionId}.",
                    sessionId);
            }
        }

        private async Task TryReleaseCreateLockAsync(
            Guid ownerUserId,
            ClassicCitySetupSessionLockHandle? createLockHandle,
            CancellationToken cancellationToken)
        {
            if (createLockHandle is null)
                return;

            try
            {
                await sessionStore.ReleaseCreateLockAsync(
                    ownerUserId: ownerUserId,
                    lockHandle: createLockHandle,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException ||
                                       !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception: ex,
                    message: "Classic City setup session create-lock release failed for ownerUserId={OwnerUserId}.",
                    ownerUserId);
            }
        }
    }
}
