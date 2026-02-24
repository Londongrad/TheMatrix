import {useCallback, useEffect, useState} from "react";
import {Link, useLocation, useNavigate, useParams} from "react-router-dom";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Button from "@shared/ui/controls/Button/Button";
import {
    getCity,
    getCityProvisioning,
} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import type {
    CityPopulationBootstrapView,
    CityProvisioningStatusView,
    CityProvisioningView,
    CityView,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
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
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import "@services/citycore/scenarios/styles/scenario-setup.css";

type ProvisioningLocationState = {
    provisioning?: CityProvisioningView;
    launchedFromSetup?: boolean;
};

export default function ClassicCityProvisioningPage() {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();
    const location = useLocation();
    const {can} = usePermissions();
    const provisioningMutations = useCityProvisioning();
    const [city, setCity] = useState<CityView | null>(null);
    const [provisioning, setProvisioning] = useState<CityProvisioningStatusView | null>(null);
    const [pageError, setPageError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [bootstrapResult, setBootstrapResult] = useState<CityPopulationBootstrapView | null>(
        (location.state as ProvisioningLocationState | null)?.provisioning?.populationBootstrap ?? null,
    );

    const canRetryBootstrap = can(PermissionKeys.CityCoreClassicCityPopulationBootstrapRetry);
    const cityStatusTone = getCityStatusTone(city?.status, city?.archivedAtUtc);
    const cityStatusLabel = formatCityStatusLabel(city?.status, city?.archivedAtUtc);
    const bootstrapOutcome = getBootstrapOutcome(bootstrapResult, provisioning);
    const failureCode = bootstrapResult?.failureCode ?? provisioning?.populationBootstrapFailureCode;
    const summary = bootstrapResult?.summary ?? null;

    const refreshProvisioningState = useCallback(async (
        options?: {
            signal?: AbortSignal;
            showLoading?: boolean;
            fallbackMessage?: string;
        },
    ) => {
        if (!cityId) {
            return;
        }

        const showLoading = options?.showLoading ?? false;
        const fallbackMessage = options?.fallbackMessage ?? "Failed to refresh provisioning state.";

        try {
            if (showLoading) {
                setIsLoading(true);
            } else {
                setIsRefreshing(true);
            }

            setPageError(null);

            const [cityView, provisioningView] = await Promise.all([
                getCity(cityId, options?.signal),
                getCityProvisioning(cityId, options?.signal),
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
    }, [cityId]);

    useEffect(() => {
        if (!cityId) {
            setPageError("City provisioning context is missing.");
            setIsLoading(false);
            return;
        }

        const abortController = new AbortController();
        void refreshProvisioningState({
            signal: abortController.signal,
            showLoading: true,
            fallbackMessage: "Failed to load provisioning state.",
        });

        return () => {
            abortController.abort();
        };
    }, [cityId, refreshProvisioningState]);

    useEffect(() => {
        if (!cityId || pageError || isLoading || isRefreshing || provisioningMutations.isSubmitting) {
            return;
        }

        if (bootstrapOutcome !== "pending" && cityStatusTone !== "provisioning") {
            return;
        }

        const timer = window.setTimeout(() => {
            void refreshProvisioningState();
        }, 5000);

        return () => {
            window.clearTimeout(timer);
        };
    }, [
        bootstrapOutcome,
        cityId,
        cityStatusTone,
        isLoading,
        isRefreshing,
        pageError,
        provisioningMutations.isSubmitting,
        refreshProvisioningState,
    ]);

    async function handleRetry() {
        if (!cityId) {
            return;
        }

        const result = await provisioningMutations.retry(cityId);
        if (!result) {
            return;
        }

        setBootstrapResult(result.populationBootstrap);
        await refreshProvisioningState({
            fallbackMessage: "Failed to refresh provisioning state after retry.",
        });
    }

    return (
        <section className="scenario-setup scenario-setup--provisioning">
            <header className="scenario-setup__hero">
                <div className="scenario-setup__eyebrow">Provisioning handoff</div>
                <div className="scenario-setup__hero-grid">
                    <div className="scenario-setup__hero-copy">
                        <h1 className="scenario-setup__title">
                            {city?.name ?? "Classic City provisioning"}
                        </h1>
                        <p className="scenario-setup__subtitle">
                            Launch outcome is handled as an explicit handoff stage. This screen keeps provisioning
                            visible, lets operators retry failed bootstrap paths, and only then hands off to the live
                            city workspace.
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
                    <LoadingIndicator label="Loading provisioning result..."/>
                </div>
            ) : null}

            {!isLoading ? (
                <div className="scenario-setup__layout">
                    <div className="scenario-setup__panel">
                        <div className="scenario-setup__panel-header">
                            <div>
                                <div className="scenario-setup__panel-eyebrow">Launch result</div>
                                <h2 className="scenario-setup__panel-title">
                                    {bootstrapOutcome === "completed"
                                        ? "City is ready for monitoring"
                                        : bootstrapOutcome === "failed"
                                            ? "Population bootstrap failed"
                                            : bootstrapOutcome === "skipped"
                                                ? "City launched without population bootstrap"
                                                : "Provisioning is still settling"}
                                </h2>
                            </div>

                            <Link className="scenario-setup__ghost-link" to={CLASSIC_CITY_LIST_PATH}>
                                Back to registry
                            </Link>
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

                        <div className="scenario-setup__review-grid">
                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">City status</span>
                                <strong className="scenario-setup__review-value">{cityStatusLabel}</strong>
                                <span className="scenario-setup__review-text">
                                    {city ? formatSimulationKindLabel(city.simulationKind) : "Classic City"}
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
                                    Operation {bootstrapResult?.operationId ?? provisioning?.populationBootstrapOperationId ?? "--"}
                                </span>
                            </article>

                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">Completed at</span>
                                <strong className="scenario-setup__review-value">
                                    {formatProvisioningDateTime(provisioning?.populationBootstrapCompletedAtUtc)}
                                </strong>
                                <span className="scenario-setup__review-text">
                                    Failed at {formatProvisioningDateTime(provisioning?.populationBootstrapFailedAtUtc)}
                                </span>
                            </article>

                            <article className="scenario-setup__review-card">
                                <span className="scenario-setup__review-label">Failure code</span>
                                <strong className="scenario-setup__review-value">
                                    {bootstrapOutcome === "failed" ? formatProvisioningFailureCode(failureCode) : "--"}
                                </strong>
                                <span className="scenario-setup__review-text">
                                    Population bootstrap is the only downstream launch stage currently retriable from UI.
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

                            <Button
                                type="button"
                                variant="default"
                                onClick={() => void refreshProvisioningState()}
                                disabled={isLoading || isRefreshing || provisioningMutations.isSubmitting}
                            >
                                {isRefreshing ? "Refreshing..." : "Refresh status"}
                            </Button>

                            {bootstrapOutcome === "failed" && canRetryBootstrap ? (
                                <Button
                                    type="button"
                                    variant="primary"
                                    onClick={() => void handleRetry()}
                                    disabled={provisioningMutations.isSubmitting}
                                >
                                    {provisioningMutations.isSubmitting ? "Retrying..." : "Retry population bootstrap"}
                                </Button>
                            ) : null}

                            {(bootstrapOutcome === "completed" || cityStatusTone === "active") ? (
                                <Button
                                    type="button"
                                    variant="success"
                                    onClick={() => navigate(getClassicCityDetailsPath(cityId))}
                                >
                                    Open live monitoring
                                </Button>
                            ) : null}
                        </div>
                    </div>

                    <aside className="scenario-setup__aside">
                        <div className={`scenario-setup__aside-card scenario-setup__aside-card--status-${cityStatusTone}`}>
                            <div className="scenario-setup__aside-label">Provisioning state</div>
                            <div className="scenario-setup__aside-value">{cityStatusLabel}</div>
                            <div className="scenario-setup__aside-list">
                                <div className="scenario-setup__aside-item">
                                    <span>City</span>
                                    <strong>{city?.name ?? cityId}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Simulation</span>
                                    <strong>{city ? formatSimulationKindLabel(city.simulationKind) : "Classic City"}</strong>
                                </div>
                                <div className="scenario-setup__aside-item">
                                    <span>Provisioning</span>
                                    <strong>
                                        {bootstrapOutcome === "completed"
                                            ? "Ready for monitoring"
                                            : bootstrapOutcome === "failed"
                                                ? "Needs retry"
                                                : "Waiting for outcome"}
                                    </strong>
                                </div>
                            </div>
                        </div>

                        <div className="scenario-setup__aside-card scenario-setup__aside-card--accent">
                            <div className="scenario-setup__aside-label">Why this screen exists</div>
                            <p className="scenario-setup__aside-copy">
                                The registry and live city workspace should not be the only place where launch failures
                                surface. Provisioning stays explicit here so operators can see what happened before they
                                move into monitoring.
                            </p>
                        </div>
                    </aside>
                </div>
            ) : null}
        </section>
    );
}
