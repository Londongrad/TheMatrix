import {useCallback, useEffect, useState} from "react";
import {Link, useLocation, useNavigate, useParams} from "react-router-dom";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Button from "@shared/ui/controls/Button/Button";
import ProvisioningTimeline, {
    type ProvisioningTimelineItem,
} from "@services/citycore/scenarios/classic-city/components/ProvisioningTimeline";
import {
    getCity,
    getCityProvisioning,
} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import {getClassicCitySetupSession} from "@services/citycore/scenarios/classic-city/api/setupSessionsApi";
import type {
    CityPopulationBootstrapView,
    CityProvisioningStatusView,
    CityView,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import type {ClassicCitySetupSessionView} from "@services/citycore/scenarios/classic-city/contracts/setupSessionContracts";
import {useCityProvisioning} from "@services/citycore/scenarios/classic-city/hooks/useCityProvisioning";
import {
    formatCityStatusLabel,
    formatProvisioningFailureCode,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import {
    formatProvisioningDateTime,
    getBootstrapOutcome,
} from "@services/citycore/scenarios/classic-city/utils/provisioning";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCitySetupSessionPath,
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import "@services/citycore/scenarios/styles/scenario-setup.css";

type ProvisioningSessionLocationState = {
    session?: ClassicCitySetupSessionView;
};

function formatSetupSessionStatusLabel(status?: string | null): string {
    switch (status) {
        case "Draft":
            return "Draft";
        case "LaunchQueued":
            return "Launch queued";
        case "CreatingCity":
            return "Creating city";
        case "BootstrappingPopulation":
            return "Bootstrapping population";
        case "Ready":
            return "Ready";
        case "ProvisioningFailed":
            return "Provisioning failed";
        case "LaunchFailed":
            return "Launch failed";
        default:
            return "Provisioning session";
    }
}

function getSetupSessionTone(status?: string | null): "draft" | "running" | "ready" | "failed" {
    switch (status) {
        case "Ready":
            return "ready";
        case "LaunchQueued":
        case "CreatingCity":
        case "BootstrappingPopulation":
            return "running";
        case "ProvisioningFailed":
        case "LaunchFailed":
            return "failed";
        default:
            return "draft";
    }
}

function getResultTitle(
    session: ClassicCitySetupSessionView | null,
    bootstrapOutcome: ReturnType<typeof getBootstrapOutcome>,
    cityStatusTone?: string,
): string {
    if (session?.status === "LaunchFailed") {
        return "Launch failed before city creation";
    }

    if (bootstrapOutcome === "completed" || cityStatusTone === "active" || session?.status === "Ready") {
        return "City is ready for monitoring";
    }

    if (bootstrapOutcome === "failed" || session?.status === "ProvisioningFailed") {
        return "Population bootstrap failed";
    }

    return "Provisioning is still in progress";
}

function getResultCopy(
    session: ClassicCitySetupSessionView | null,
    bootstrapOutcome: ReturnType<typeof getBootstrapOutcome>,
): string {
    if (session?.status === "LaunchFailed") {
        return session.failureMessage ?? "Gateway could not create the city host from the queued setup draft.";
    }

    if (bootstrapOutcome === "failed" || session?.status === "ProvisioningFailed") {
        return session?.failureMessage ?? "City creation completed, but downstream population bootstrap requires operator review.";
    }

    if (bootstrapOutcome === "completed" || session?.status === "Ready") {
        return "The launch contract has finished, and the host can now move into live monitoring.";
    }

    return "This screen stays attached to the setup session and shows explicit orchestration progress until the handoff is complete.";
}

function getEffectiveBootstrap(
    sessionBootstrap: CityPopulationBootstrapView | null,
    retryBootstrap: CityPopulationBootstrapView | null,
    provisioning: CityProvisioningStatusView | null,
): CityPopulationBootstrapView | null {
    const provisioningStatus = provisioning?.status?.toLowerCase();

    if (provisioningStatus === "active") {
        if (retryBootstrap?.status?.toLowerCase() === "completed") {
            return retryBootstrap;
        }

        if (sessionBootstrap?.status?.toLowerCase() === "completed") {
            return sessionBootstrap;
        }

        return null;
    }

    if (provisioningStatus === "provisioningfailed") {
        if (retryBootstrap?.status?.toLowerCase() === "failed") {
            return retryBootstrap;
        }

        if (sessionBootstrap?.status?.toLowerCase() === "failed") {
            return sessionBootstrap;
        }
    }

    return retryBootstrap ?? sessionBootstrap;
}

function getTimelineItems(args: {
    session: ClassicCitySetupSessionView | null;
    bootstrapOutcome: ReturnType<typeof getBootstrapOutcome>;
    failureCode?: string | null;
    cityName?: string | null;
    cityStatusTone?: string;
    currentBootstrap?: CityPopulationBootstrapView | null;
}): ProvisioningTimelineItem[] {
    const {
        session,
        bootstrapOutcome,
        failureCode,
        cityName,
        cityStatusTone,
        currentBootstrap,
    } = args;
    const hasCity = Boolean(session?.cityId);
    const launchFailed = session?.status === "LaunchFailed";
    const provisioningFailed = bootstrapOutcome === "failed" || session?.status === "ProvisioningFailed";
    const ready = bootstrapOutcome === "completed" || cityStatusTone === "active" || session?.status === "Ready";

    return [
        {
            id: "creating-city",
            title: "Creating city",
            description: hasCity
                ? `${cityName ?? "City host"} was created in CityCore with topology, clock, and initial environment.`
                : launchFailed
                    ? "Gateway failed before a city host could be created."
                    : "Gateway is creating the city host from the queued setup session.",
            status: launchFailed
                ? "failed"
                : hasCity || session?.status === "BootstrappingPopulation" || session?.status === "Ready" || session?.status === "ProvisioningFailed"
                    ? "complete"
                    : session?.status === "LaunchQueued" || session?.status === "CreatingCity"
                        ? "current"
                        : "pending",
            meta: session?.startedAtUtc
                ? `Started ${formatProvisioningDateTime(session.startedAtUtc)}`
                : session?.launchQueuedAtUtc
                    ? `Queued ${formatProvisioningDateTime(session.launchQueuedAtUtc)}`
                    : undefined,
        },
        {
            id: "bootstrapping-population",
            title: "Bootstrapping population",
            description: provisioningFailed
                ? "Population initialization failed after city creation and requires operator attention."
                : ready
                    ? "Population bootstrap completed and the city is ready to hand off."
                    : hasCity
                        ? "Population service is initializing residents, households, and the first settlement snapshot."
                        : "Population bootstrap cannot begin until city creation completes.",
            status: provisioningFailed
                ? "failed"
                : ready
                    ? "complete"
                    : session?.status === "BootstrappingPopulation" || (hasCity && bootstrapOutcome === "pending")
                        ? "current"
                        : "pending",
            meta: currentBootstrap?.operationId
                ? `Operation ${currentBootstrap.operationId}`
                : session?.cityId
                    ? `City ${session.cityId.slice(0, 8)}`
                    : undefined,
        },
        {
            id: "handoff-result",
            title: ready ? "Ready for monitoring" : "Provisioning outcome",
            description: ready
                ? "Provisioning finished successfully. Operators can now move into the live city workspace."
                : launchFailed
                    ? "The setup draft remains editable because launch failed before the host was created."
                    : provisioningFailed
                        ? "City creation succeeded, but the provisioning handoff is blocked on a failed bootstrap stage."
                        : "Final handoff stays pending until city creation and bootstrap both report a terminal outcome.",
            status: ready
                ? "complete"
                : launchFailed || provisioningFailed
                    ? "failed"
                    : hasCity || session?.status === "CreatingCity" || session?.status === "BootstrappingPopulation"
                        ? "current"
                        : "pending",
            meta: ready
                ? `Completed ${formatProvisioningDateTime(session?.completedAtUtc)}`
                : failureCode
                    ? `Failure ${formatProvisioningFailureCode(failureCode)}`
                    : session?.completedAtUtc
                        ? `Last update ${formatProvisioningDateTime(session.completedAtUtc)}`
                        : undefined,
        },
    ];
}

export default function ClassicCityProvisioningSessionPage() {
    const params = useParams<{ sessionId: string }>();
    const sessionId = params.sessionId ?? "";
    const navigate = useNavigate();
    const location = useLocation();
    const initialSession = (location.state as ProvisioningSessionLocationState | null)?.session ?? null;
    const {can} = usePermissions();
    const provisioningMutations = useCityProvisioning();
    const [session, setSession] = useState<ClassicCitySetupSessionView | null>(initialSession);
    const [city, setCity] = useState<CityView | null>(null);
    const [provisioning, setProvisioning] = useState<CityProvisioningStatusView | null>(null);
    const [retryBootstrap, setRetryBootstrap] = useState<CityPopulationBootstrapView | null>(null);
    const [pageError, setPageError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(initialSession === null);
    const [isRefreshing, setIsRefreshing] = useState(false);

    const canInspectLiveCity = can(PermissionKeys.CityCoreClassicCityRead) && can(PermissionKeys.CityCoreSimulationRead);
    const canRetryBootstrap = can(PermissionKeys.CityCoreClassicCityPopulationBootstrapRetry) && canInspectLiveCity;

    const sessionBootstrap = session?.provisioning?.populationBootstrap ?? null;
    const effectiveBootstrap = getEffectiveBootstrap(sessionBootstrap, retryBootstrap, provisioning);
    const bootstrapOutcome = getBootstrapOutcome(effectiveBootstrap, provisioning);
    const failureCode = effectiveBootstrap?.failureCode ?? provisioning?.populationBootstrapFailureCode ?? session?.failureCode;
    const summary = effectiveBootstrap?.summary ?? null;
    const cityStatusTone = getCityStatusTone(city?.status, city?.archivedAtUtc);
    const cityStatusLabel = formatCityStatusLabel(city?.status, city?.archivedAtUtc);
    const sessionStatusLabel = formatSetupSessionStatusLabel(session?.status);
    const sessionTone = getSetupSessionTone(session?.status);
    const resultTitle = getResultTitle(session, bootstrapOutcome, cityStatusTone);
    const resultCopy = getResultCopy(session, bootstrapOutcome);
    const timelineItems = getTimelineItems({
        session,
        bootstrapOutcome,
        failureCode,
        cityName: city?.name ?? session?.draft.name ?? null,
        cityStatusTone,
        currentBootstrap: effectiveBootstrap,
    });

    const refreshProvisioningSession = useCallback(async (
        options?: {
            signal?: AbortSignal;
            showLoading?: boolean;
            fallbackMessage?: string;
        },
    ) => {
        if (!sessionId) {
            return;
        }

        const showLoading = options?.showLoading ?? false;
        const fallbackMessage = options?.fallbackMessage ?? "Failed to refresh provisioning session.";

        try {
            if (showLoading) {
                setIsLoading(true);
            } else {
                setIsRefreshing(true);
            }

            setPageError(null);

            const nextSession = await getClassicCitySetupSession(sessionId, options?.signal);
            if (options?.signal?.aborted) {
                return;
            }

            setSession(nextSession);

            if (!nextSession.cityId || !canInspectLiveCity) {
                setCity(null);
                setProvisioning(null);
                return;
            }

            const [cityView, provisioningView] = await Promise.all([
                getCity(nextSession.cityId, options?.signal),
                getCityProvisioning(nextSession.cityId, options?.signal),
            ]);

            if (options?.signal?.aborted) {
                return;
            }

            setCity(cityView);
            setProvisioning(provisioningView);
        } catch (error: unknown) {
            if (options?.signal?.aborted) {
                return;
            }

            const message = error instanceof Error && error.message.trim().length > 0
                ? error.message
                : fallbackMessage;
            setPageError(message);
        } finally {
            if (options?.signal?.aborted) {
                return;
            }

            if (showLoading) {
                setIsLoading(false);
            } else {
                setIsRefreshing(false);
            }
        }
    }, [canInspectLiveCity, sessionId]);

    useEffect(() => {
        if (!sessionId) {
            setPageError("Provisioning session id is missing.");
            setIsLoading(false);
            return;
        }

        const abortController = new AbortController();
        void refreshProvisioningSession({
            signal: abortController.signal,
            showLoading: initialSession === null,
            fallbackMessage: "Failed to load provisioning session.",
        });

        return () => {
            abortController.abort();
        };
    }, [initialSession, refreshProvisioningSession, sessionId]);

    useEffect(() => {
        if (!sessionId || pageError || isLoading || isRefreshing || provisioningMutations.isSubmitting) {
            return;
        }

        if (
            session?.status !== "LaunchQueued" &&
            session?.status !== "CreatingCity" &&
            session?.status !== "BootstrappingPopulation" &&
            !(session?.cityId && bootstrapOutcome === "pending")
        ) {
            return;
        }

        const timer = window.setTimeout(() => {
            void refreshProvisioningSession();
        }, 2500);

        return () => {
            window.clearTimeout(timer);
        };
    }, [
        bootstrapOutcome,
        isLoading,
        isRefreshing,
        pageError,
        provisioningMutations.isSubmitting,
        refreshProvisioningSession,
        session?.cityId,
        session?.status,
        sessionId,
    ]);

    async function handleRetry() {
        if (!session?.cityId) {
            return;
        }

        const result = await provisioningMutations.retry(session.cityId);
        if (!result) {
            return;
        }

        setRetryBootstrap(result.populationBootstrap);
        await refreshProvisioningSession({
            fallbackMessage: "Failed to refresh provisioning session after retry.",
        });
    }

    return (
        <section className="scenario-setup scenario-setup--provisioning">
            <header className="scenario-setup__hero">
                <div className="scenario-setup__eyebrow">Provisioning session</div>
                <div className="scenario-setup__hero-grid">
                    <div className="scenario-setup__hero-copy">
                        <div className="scenario-setup__status-row">
                            <span className={`scenario-setup__status-chip scenario-setup__status-chip--${sessionTone}`}>
                                {sessionStatusLabel}
                            </span>
                            {session?.sessionId ? (
                                <span className="scenario-setup__status-meta">
                                    Session {session.sessionId.slice(0, 8)}
                                </span>
                            ) : null}
                        </div>

                        <h1 className="scenario-setup__title">
                            {(city?.name ?? session?.draft.name) || "Classic City provisioning"}
                        </h1>
                        <p className="scenario-setup__subtitle">
                            This handoff stays attached to the setup session itself, so launch progress remains visible
                            before a city id exists and still makes sense after refresh or tab restore.
                        </p>
                    </div>

                    <div className="scenario-setup__hero-art" aria-hidden="true">
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--one"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--two"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--three"/>
                    </div>
                </div>
            </header>

            {isLoading ? (
                <div className="scenario-setup__panel">
                    <LoadingIndicator label="Loading provisioning session..."/>
                </div>
            ) : null}

            {!isLoading ? (
                <div className="scenario-setup__layout">
                    <div className="scenario-setup__panel">
                        <div className="scenario-setup__panel-header">
                            <div>
                                <div className="scenario-setup__panel-eyebrow">Provisioning timeline</div>
                                <h2 className="scenario-setup__panel-title">{resultTitle}</h2>
                            </div>

                            <Link className="scenario-setup__ghost-link" to={CLASSIC_CITY_LIST_PATH}>
                                Back to registry
                            </Link>
                        </div>

                        <div className="scenario-setup__note">
                            {resultCopy}
                        </div>

                        {pageError ? (
                            <div className="scenario-setup__error-banner" role="alert">
                                {pageError}
                            </div>
                        ) : null}

                        {provisioningMutations.error ? (
                            <div className="scenario-setup__error-banner" role="alert">
                                {provisioningMutations.error}
                            </div>
                        ) : null}

                        <ProvisioningTimeline items={timelineItems}/>

                        <div className="scenario-setup__review-grid">
                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">Session status</span>
                                <strong className="scenario-setup__review-value">{sessionStatusLabel}</strong>
                                <span className="scenario-setup__review-text">
                                    {session?.scenarioKind ?? "Classic City"} orchestration resource
                                </span>
                            </article>

                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">City host</span>
                                <strong className="scenario-setup__review-value">
                                    {session?.cityId ? (city?.name ?? session.cityId) : "Not created yet"}
                                </strong>
                                <span className="scenario-setup__review-text">
                                    {city ? formatSimulationKindLabel(city.simulationKind) : "Waiting for CityCore handoff"}
                                </span>
                            </article>

                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">Bootstrap result</span>
                                <strong className="scenario-setup__review-value">
                                    {bootstrapOutcome === "completed"
                                        ? "Completed"
                                        : bootstrapOutcome === "failed"
                                            ? "Failed"
                                            : bootstrapOutcome === "skipped"
                                                ? "Skipped"
                                                : "Pending"}
                                </strong>
                                <span className="scenario-setup__review-text">
                                    {effectiveBootstrap?.operationId
                                        ? `Operation ${effectiveBootstrap.operationId}`
                                        : "Operation id will appear after bootstrap starts"}
                                </span>
                            </article>

                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">Failure code</span>
                                <strong className="scenario-setup__review-value">
                                    {failureCode ? formatProvisioningFailureCode(failureCode) : "--"}
                                </strong>
                                <span className="scenario-setup__review-text">
                                    {session?.status === "LaunchFailed"
                                        ? "Launch failed before the city host existed."
                                        : "Population bootstrap is the only downstream stage currently retriable from UI."}
                                </span>
                            </article>
                        </div>

                        {summary ? (
                            <div className="scenario-setup__stats-grid">
                                <div className="scenario-setup__stat-card">
                                    <span className="scenario-setup__stat-label">Requested people</span>
                                    <strong className="scenario-setup__stat-value">{summary.requestedPeopleCount}</strong>
                                </div>
                                <div className="scenario-setup__stat-card">
                                    <span className="scenario-setup__stat-label">Generated people</span>
                                    <strong className="scenario-setup__stat-value">{summary.generatedPeopleCount}</strong>
                                </div>
                                <div className="scenario-setup__stat-card">
                                    <span className="scenario-setup__stat-label">Households</span>
                                    <strong className="scenario-setup__stat-value">{summary.householdCount}</strong>
                                </div>
                                <div className="scenario-setup__stat-card">
                                    <span className="scenario-setup__stat-label">Housed / homeless</span>
                                    <strong className="scenario-setup__stat-value">
                                        {summary.housedPeopleCount} / {summary.homelessPeopleCount}
                                    </strong>
                                </div>
                            </div>
                        ) : null}

                        <div className="scenario-setup__actions">
                            <Link className="scenario-setup__secondary-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                                Compose another scenario
                            </Link>

                            {session?.status === "LaunchFailed" ? (
                                <Button
                                    type="button"
                                    variant="default"
                                    onClick={() => navigate(getClassicCitySetupSessionPath(session.sessionId))}
                                >
                                    Return to setup draft
                                </Button>
                            ) : null}

                            <Button
                                type="button"
                                variant="default"
                                onClick={() => void refreshProvisioningSession()}
                                disabled={isRefreshing || provisioningMutations.isSubmitting}
                            >
                                {isRefreshing ? "Refreshing..." : "Refresh status"}
                            </Button>

                            {session?.cityId && bootstrapOutcome === "failed" && canRetryBootstrap ? (
                                <Button
                                    type="button"
                                    variant="primary"
                                    onClick={() => void handleRetry()}
                                    disabled={provisioningMutations.isSubmitting}
                                >
                                    {provisioningMutations.isSubmitting ? "Retrying..." : "Retry population bootstrap"}
                                </Button>
                            ) : null}

                            {session?.cityId && (bootstrapOutcome === "completed" || cityStatusTone === "active") && canInspectLiveCity ? (
                                <Button
                                    type="button"
                                    variant="success"
                                    onClick={() => navigate(getClassicCityDetailsPath(session.cityId!))}
                                >
                                    Open live monitoring
                                </Button>
                            ) : null}
                        </div>
                    </div>

                    <aside className="scenario-setup__aside">
                        <div className={`scenario-setup__aside-card scenario-setup__aside-card--status-${sessionTone}`}>
                            <div className="scenario-setup__aside-label">Session context</div>
                            <div className="scenario-setup__aside-value">{sessionStatusLabel}</div>
                            <div className="scenario-setup__aside-list">
                                <div className="scenario-setup__aside-item">
                                    <span>Draft</span>
                                    <strong>{session?.draft.name || "Classic City launch"}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Session id</span>
                                    <strong>{session?.sessionId ?? sessionId}</strong>
                                </div>
                                {session?.cityId ? (
                                    <div className="scenario-setup__aside-item">
                                        <span>City id</span>
                                        <strong>{session.cityId}</strong>
                                    </div>
                                ) : null}
                                {city ? (
                                    <div className="scenario-setup__aside-item">
                                        <span>City status</span>
                                        <strong>{cityStatusLabel}</strong>
                                    </div>
                                ) : null}
                            </div>
                        </div>

                        <div className="scenario-setup__aside-card scenario-setup__aside-card--accent">
                            <div className="scenario-setup__aside-label">Lifecycle timestamps</div>
                            <div className="scenario-setup__aside-list">
                                <div className="scenario-setup__aside-item">
                                    <span>Created</span>
                                    <strong>{formatProvisioningDateTime(session?.createdAtUtc)}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Queued</span>
                                    <strong>{formatProvisioningDateTime(session?.launchQueuedAtUtc)}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Started</span>
                                    <strong>{formatProvisioningDateTime(session?.startedAtUtc)}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Completed</span>
                                    <strong>{formatProvisioningDateTime(session?.completedAtUtc)}</strong>
                                </div>
                            </div>
                        </div>

                        <div className="scenario-setup__aside-card">
                            <div className="scenario-setup__aside-label">Operational note</div>
                            <p className="scenario-setup__aside-copy">
                                {canInspectLiveCity
                                    ? "This screen merges setup-session orchestration with live provisioning signals once a city id exists."
                                    : "This screen stays functional from setup-session data even when live city read permissions are not available."}
                            </p>

                            {(session?.status === "LaunchQueued" ||
                                session?.status === "CreatingCity" ||
                                session?.status === "BootstrappingPopulation" ||
                                (session?.cityId && bootstrapOutcome === "pending")) ? (
                                <LoadingIndicator label="Refreshing provisioning timeline..."/>
                            ) : null}
                        </div>
                    </aside>
                </div>
            ) : null}
        </section>
    );
}
