using System.Net;
using MassTransit;
using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionService(
        IClassicCitySetupSessionStore sessionStore,
        ICityProvisioningService provisioningService,
        IPublishEndpoint publishEndpoint,
        ILogger<ClassicCitySetupSessionService> logger)
        : IClassicCitySetupSessionService
    {
        private const string PopulationModeAutomatic = "automatic";
        private const string PopulationModeManual = "manual";
        private const int MaxPlannedPeopleCount = 1_000_000;

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
        }

        public async Task<ClassicCitySetupSessionMutationResult> QueueLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
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

            await publishEndpoint.Publish(
                message: new ClassicCitySetupLaunchRequested(session.SessionId),
                cancellationToken: cancellationToken);

            return Updated(session);
        }

        public async Task ProcessLaunchAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ClassicCitySetupSessionState? session = await sessionStore.GetAsync(
                sessionId: sessionId,
                cancellationToken: cancellationToken);

            if (session is null || !string.Equals(session.Status, ClassicCitySetupSessionStatuses.LaunchQueued, StringComparison.Ordinal))
                return;

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
            session.StartedAtUtc = startedAtUtc;
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
                PopulationMode = NormalizePopulationMode(draft.PopulationMode),
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

            if (string.Equals(draft.PopulationMode, PopulationModeManual, StringComparison.Ordinal))
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
                PlannedPeopleCount: plannedPeopleCount);

            return true;
        }

        private static string NormalizePopulationMode(string? value)
        {
            return string.Equals(value?.Trim(), PopulationModeManual, StringComparison.OrdinalIgnoreCase)
                ? PopulationModeManual
                : PopulationModeAutomatic;
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
    }
}
