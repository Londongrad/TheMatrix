import {useEffect, useMemo} from "react";
import {Link, useNavigate} from "react-router-dom";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import {useCitiesQuery} from "@services/citycore/scenarios/classic-city/hooks/useCitiesQuery";
import {useProvisioningCitiesQuery} from "@services/citycore/scenarios/classic-city/hooks/useProvisioningCitiesQuery";
import {
    describeCityLifecycle,
    formatCityShortId,
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCitySetupPath,
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/dashboard/styles/dashboard.css";

const DASHBOARD_AUTO_REFRESH_MS = 30000;

function getProvisioningRank(city: CityListItemView): number {
    switch (getCityStatusTone(city.status)) {
        case "failed":
            return 0;
        case "provisioning":
            return 1;
        default:
            return 2;
    }
}

function sortAlphabetically(cities: CityListItemView[]): CityListItemView[] {
    return [...cities].sort((left, right) =>
        left.name.localeCompare(right.name, undefined, {sensitivity: "base"}),
    );
}

function sortProvisioningQueue(cities: CityListItemView[]): CityListItemView[] {
    return [...cities].sort((left, right) => {
        const rankDelta = getProvisioningRank(left) - getProvisioningRank(right);

        if (rankDelta !== 0) {
            return rankDelta;
        }

        return left.name.localeCompare(right.name, undefined, {sensitivity: "base"});
    });
}

type WatchlistProps = {
    title: string;
    subtitle: string;
    emptyTitle: string;
    emptyText: string;
    cities: CityListItemView[];
    onOpen: (city: CityListItemView) => void;
    actionLabel?: (city: CityListItemView) => string;
};

function WatchlistSection({
    title,
    subtitle,
    emptyTitle,
    emptyText,
    cities,
    onOpen,
    actionLabel,
}: WatchlistProps) {
    return (
        <section className="dashboard-panel">
            <div className="dashboard-panel__header">
                <div>
                    <h2 className="dashboard-panel__title">{title}</h2>
                    <p className="dashboard-panel__subtitle">{subtitle}</p>
                </div>
                <span className="settings-pill">{cities.length}</span>
            </div>

            {cities.length === 0 ? (
                <div className="dashboard-empty-state" role="status">
                    <div className="dashboard-empty-state__title">{emptyTitle}</div>
                    <div className="dashboard-empty-state__text">{emptyText}</div>
                </div>
            ) : (
                <div className="dashboard-watchlist">
                    {cities.map((city) => {
                        const statusTone = getCityStatusTone(city.status);

                        return (
                            <article key={city.cityId} className={`dashboard-city-row dashboard-city-row--${statusTone}`}>
                                <div className="dashboard-city-row__main">
                                    <div className="dashboard-city-row__topline">
                                        <span className={`cities-status-pill cities-status-pill--${statusTone}`}>
                                            {formatCityStatusLabel(city.status)}
                                        </span>
                                        <span className="dashboard-city-row__id" title={city.cityId}>
                                            {formatCityShortId(city.cityId)}
                                        </span>
                                    </div>

                                    <h3 className="dashboard-city-row__title">{city.name}</h3>

                                    <div className="dashboard-city-row__meta">
                                        <span>{formatSimulationKindLabel(city.simulationKind)}</span>
                                        <span className="dashboard-city-row__separator">/</span>
                                        <span>{describeCityLifecycle(city.status, null, "registry")}</span>
                                    </div>
                                </div>

                                <div className="dashboard-city-row__actions">
                                    <Button
                                        size="sm"
                                        variant={statusTone === "archived" || statusTone === "unknown" ? "default" : "primary"}
                                        onClick={() => onOpen(city)}
                                    >
                                        {actionLabel?.(city) ?? "Open"}
                                    </Button>
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}
        </section>
    );
}

const DashboardPage = () => {
    const navigate = useNavigate();
    const {can} = usePermissions();
    const canCreateCity = can(PermissionKeys.CityCoreClassicCityCreate);

    const citiesQuery = useCitiesQuery(true);
    const provisioningQuery = useProvisioningCitiesQuery();

    useEffect(() => {
        const intervalId = window.setInterval(() => {
            if (document.visibilityState !== "visible") {
                return;
            }

            void Promise.all([
                citiesQuery.refetch(),
                provisioningQuery.refetch(),
            ]);
        }, DASHBOARD_AUTO_REFRESH_MS);

        return () => {
            window.clearInterval(intervalId);
        };
    }, [citiesQuery.refetch, provisioningQuery.refetch]);

    const readyCities = useMemo(
        () => sortAlphabetically(citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "active")),
        [citiesQuery.data],
    );
    const archivedCities = useMemo(
        () => sortAlphabetically(citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "archived")),
        [citiesQuery.data],
    );
    const attentionQueue = useMemo(
        () => sortProvisioningQueue(provisioningQuery.data),
        [provisioningQuery.data],
    );
    const failedQueue = useMemo(
        () => attentionQueue.filter((city) => getCityStatusTone(city.status) === "failed"),
        [attentionQueue],
    );

    const stats = useMemo(() => ({
        totalHosts: readyCities.length + archivedCities.length + attentionQueue.length,
        ready: readyCities.length,
        attention: attentionQueue.length,
        failed: failedQueue.length,
        archived: archivedCities.length,
    }), [archivedCities.length, attentionQueue.length, failedQueue.length, readyCities.length]);

    const isRefreshing = citiesQuery.isLoading || provisioningQuery.isLoading;

    const openCity = (city: CityListItemView) => {
        const statusTone = getCityStatusTone(city.status);
        navigate(
            statusTone === "provisioning" || statusTone === "failed"
                ? getClassicCityProvisioningPath(city.cityId)
                : getClassicCityDetailsPath(city.cityId),
        );
    };

    return (
        <section className="dashboard-page">
            <header className="dashboard-hero">
                <div className="dashboard-hero__content">
                    <div className="dashboard-hero__eyebrow">CityCore / Operations</div>
                    <h1 className="dashboard-hero__title">Simulation watchboard</h1>
                    <p className="dashboard-hero__subtitle">
                        Watch the global state of city hosts, keep failed or in-flight launches visible,
                        and jump straight into the registry or provisioning handoff without opening each city blindly.
                    </p>
                    <div className="dashboard-hero__meta">
                        <span className="settings-pill">Auto-refresh every 30s while visible</span>
                        <span className="settings-pill">{stats.totalHosts} tracked hosts</span>
                    </div>
                </div>

                <div className="dashboard-hero__actions">
                    {canCreateCity ? (
                        <Link className="cities-page__header-link cities-page__header-link--primary" to={getClassicCitySetupPath()}>
                            Compose Classic City
                        </Link>
                    ) : null}

                    <Link className="cities-page__header-link" to={CLASSIC_CITY_LIST_PATH}>
                        Open registry
                    </Link>

                    <Link className="cities-page__header-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                        Scenario catalog
                    </Link>

                    <Button
                        type="button"
                        variant="default"
                        onClick={() => {
                            void Promise.all([citiesQuery.refetch(), provisioningQuery.refetch()]);
                        }}
                        disabled={isRefreshing}
                    >
                        {isRefreshing ? "Refreshing..." : "Refresh watchboard"}
                    </Button>
                </div>
            </header>

            {(citiesQuery.error || provisioningQuery.error) ? (
                <div className="citycore-error-banner" role="alert">
                    <span>
                        {citiesQuery.error ?? provisioningQuery.error}
                    </span>
                    <Button
                        type="button"
                        variant="primary"
                        onClick={() => {
                            void Promise.all([citiesQuery.refetch(), provisioningQuery.refetch()]);
                        }}
                    >
                        Retry
                    </Button>
                </div>
            ) : null}

            <div className="dashboard-metrics" aria-label="Simulation watchboard summary">
                <article className="cities-metric-card">
                    <span className="cities-metric-card__label">Tracked hosts</span>
                    <strong className="cities-metric-card__value">{stats.totalHosts}</strong>
                    <span className="cities-metric-card__hint">
                        Combined view of ready cities, archived records, and the provisioning handoff queue.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--active">
                    <span className="cities-metric-card__label">Ready monitoring</span>
                    <strong className="cities-metric-card__value">{stats.ready}</strong>
                    <span className="cities-metric-card__hint">
                        Active city hosts already handed off to live monitoring workspaces.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--provisioning">
                    <span className="cities-metric-card__label">Attention queue</span>
                    <strong className="cities-metric-card__value">{stats.attention}</strong>
                    <span className="cities-metric-card__hint">
                        In-flight launches and unresolved handoffs that still need operator attention.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--archived">
                    <span className="cities-metric-card__label">Archived records</span>
                    <strong className="cities-metric-card__value">{stats.archived}</strong>
                    <span className="cities-metric-card__hint">
                        Historical city records kept for review, cleanup, and post-mortem inspection.
                    </span>
                </article>
            </div>

            <div className="dashboard-layout">
                <WatchlistSection
                    title="Attention queue"
                    subtitle="Provisioning handoffs and failed launches that still need active operator review."
                    emptyTitle="Attention queue is clear"
                    emptyText="No launches are currently stalled or waiting for downstream bootstrap resolution."
                    cities={attentionQueue}
                    onOpen={openCity}
                    actionLabel={(city) => getCityStatusTone(city.status) === "failed" ? "Resolve handoff" : "Open handoff"}
                />

                <WatchlistSection
                    title="Ready monitoring hosts"
                    subtitle="Active cities that are already live and available for monitoring workspaces."
                    emptyTitle="No ready cities yet"
                    emptyText="Launch a new city from the setup wizard or wait for provisioning to complete."
                    cities={readyCities.slice(0, 6)}
                    onOpen={openCity}
                    actionLabel={() => "Open monitoring"}
                />

                <WatchlistSection
                    title="Archived records"
                    subtitle="Inactive city records that remain available for audit, review, or cleanup."
                    emptyTitle="Archive is empty"
                    emptyText="Archived city records will appear here once they are taken out of active monitoring."
                    cities={archivedCities.slice(0, 4)}
                    onOpen={openCity}
                    actionLabel={() => "Review record"}
                />

                <section className="dashboard-panel dashboard-panel--notes">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Operator cadence</h2>
                            <p className="dashboard-panel__subtitle">
                                Use this watchboard as the first stop before diving into a specific city workspace.
                            </p>
                        </div>
                    </div>

                    <div className="dashboard-notes">
                        <article className="dashboard-note">
                            <span className="dashboard-note__eyebrow">Start here</span>
                            <h3 className="dashboard-note__title">Scan the queue before the registry</h3>
                            <p className="dashboard-note__text">
                                Failed or unfinished launches stay visible above so they do not get buried among ready cities.
                            </p>
                        </article>

                        <article className="dashboard-note">
                            <span className="dashboard-note__eyebrow">Monitoring</span>
                            <h3 className="dashboard-note__title">Use ready hosts for live operations</h3>
                            <p className="dashboard-note__text">
                                Open active cities from here when you already know what host needs attention and want to jump in directly.
                            </p>
                        </article>

                        <article className="dashboard-note">
                            <span className="dashboard-note__eyebrow">Archive</span>
                            <h3 className="dashboard-note__title">Keep inactive records available</h3>
                            <p className="dashboard-note__text">
                                Archived cities remain reviewable here without mixing them into the live handoff queue.
                            </p>
                        </article>
                    </div>
                </section>
            </div>
        </section>
    );
};

export default DashboardPage;
