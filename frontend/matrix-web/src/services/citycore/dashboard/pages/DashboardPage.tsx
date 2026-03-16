import {useEffect} from "react";
import {Link, useNavigate} from "react-router-dom";
import type {
    CityOperationsDashboardView,
    DashboardMetricView,
    DashboardPeriodComparisonRowView,
    DashboardRecentEventView,
    DashboardServiceHealthView,
} from "@services/citycore/dashboard/api/dashboardTypes";
import {useCityOperationsDashboardQuery} from "@services/citycore/dashboard/hooks/useCityOperationsDashboardQuery";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import {
    describeCityLifecycle,
    formatCityShortId,
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import {
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

function formatDateTime(value: string) {
    return new Intl.DateTimeFormat(document.documentElement.lang || undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    }).format(new Date(value));
}

function formatDelta(value: number | null | undefined) {
    if (value == null) {
        return null;
    }

    if (value === 0) {
        return "0";
    }

    return value > 0 ? `+${value}` : String(value);
}

function getDeltaTone(value: number) {
    if (value > 0) {
        return "positive";
    }

    if (value < 0) {
        return "negative";
    }

    return "neutral";
}

function getHealthTone(status: string) {
    switch (status.trim().toLowerCase()) {
        case "healthy":
            return "healthy";
        case "degraded":
            return "degraded";
        default:
            return "unhealthy";
    }
}

function openCityPath(cityId: string, status: string, archivedAtUtc?: string | null) {
    const tone = getCityStatusTone(status, archivedAtUtc);
    return tone === "provisioning" || tone === "failed"
        ? getClassicCityProvisioningPath(cityId)
        : getClassicCityDetailsPath(cityId);
}

type MetricCardProps = {
    metric: DashboardMetricView;
};

function MetricCard({metric}: MetricCardProps) {
    const dayDelta = formatDelta(metric.deltaYesterday);
    const monthDelta = formatDelta(metric.deltaMonth);
    const yearDelta = formatDelta(metric.deltaYear);

    return (
        <article className="dashboard-metric-card">
            <div className="dashboard-metric-card__label-row">
                <span className="dashboard-metric-card__label">{metric.label}</span>
                {metric.deltaMode === "live" ? (
                    <span className="dashboard-live-pill">Live</span>
                ) : null}
            </div>
            <strong className="dashboard-metric-card__value">{metric.current}</strong>
            <p className="dashboard-metric-card__description">{metric.description}</p>

            {metric.deltaMode === "live" ? (
                <div className="dashboard-metric-card__live-note">
                    This queue is read as a live snapshot, not as a historical total.
                </div>
            ) : (
                <div className="dashboard-metric-card__delta-grid">
                    <div className="dashboard-metric-card__delta">
                        <span className="dashboard-metric-card__delta-label">Day</span>
                        <span
                            className={`dashboard-metric-card__delta-value dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaYesterday ?? 0)}`}>
                            {dayDelta ?? "--"}
                        </span>
                    </div>
                    <div className="dashboard-metric-card__delta">
                        <span className="dashboard-metric-card__delta-label">Month</span>
                        <span
                            className={`dashboard-metric-card__delta-value dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaMonth ?? 0)}`}>
                            {monthDelta ?? "--"}
                        </span>
                    </div>
                    <div className="dashboard-metric-card__delta">
                        <span className="dashboard-metric-card__delta-label">Year</span>
                        <span
                            className={`dashboard-metric-card__delta-value dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaYear ?? 0)}`}>
                            {yearDelta ?? "--"}
                        </span>
                    </div>
                </div>
            )}
        </article>
    );
}

type ActivityRowProps = {
    row: DashboardPeriodComparisonRowView;
};

function ActivityRow({row}: ActivityRowProps) {
    const periods = [
        {label: "Yesterday", value: row.yesterday},
        {label: "Month", value: row.month},
        {label: "Year", value: row.year},
    ];

    return (
        <article className="dashboard-activity-row">
            <div className="dashboard-activity-row__summary">
                <h3 className="dashboard-activity-row__title">{row.label}</h3>
                <p className="dashboard-activity-row__description">{row.description}</p>
            </div>

            <div className="dashboard-activity-row__periods">
                {periods.map((period) => (
                    <div key={period.label} className="dashboard-activity-period">
                        <div className="dashboard-activity-period__label">{period.label}</div>
                        <div className="dashboard-activity-period__numbers">
                            <span className="dashboard-activity-period__current">{period.value.current}</span>
                            <span className="dashboard-activity-period__separator">/</span>
                            <span className="dashboard-activity-period__previous">{period.value.previous}</span>
                        </div>
                        <div
                            className={`dashboard-activity-period__delta dashboard-activity-period__delta--${getDeltaTone(period.value.delta)}`}>
                            {formatDelta(period.value.delta)}
                        </div>
                    </div>
                ))}
            </div>
        </article>
    );
}

type HealthItemProps = {
    service: DashboardServiceHealthView;
};

function HealthItem({service}: HealthItemProps) {
    const tone = getHealthTone(service.status);

    return (
        <article className={`dashboard-health-item dashboard-health-item--${tone}`}>
            <div className="dashboard-health-item__topline">
                <h3 className="dashboard-health-item__service">{service.service}</h3>
                <span className={`dashboard-health-pill dashboard-health-pill--${tone}`}>
                    {service.status}
                </span>
            </div>
            <p className="dashboard-health-item__detail">{service.detail}</p>
            <span className="dashboard-health-item__timestamp">{formatDateTime(service.checkedAtUtc)}</span>
        </article>
    );
}

type EventItemProps = {
    event: DashboardRecentEventView;
    onOpen: (cityId: string, status: string) => void;
};

function EventItem({event, onOpen}: EventItemProps) {
    return (
        <article className={`dashboard-event dashboard-event--${event.severity}`}>
            <div className="dashboard-event__topline">
                <div>
                    <h3 className="dashboard-event__title">{event.title}</h3>
                    <div className="dashboard-event__meta">
                        <span>{event.cityName}</span>
                        <span className="dashboard-event__separator">/</span>
                        <span>{formatDateTime(event.occurredAtUtc)}</span>
                    </div>
                </div>

                <Button
                    size="sm"
                    variant={event.severity === "danger" ? "danger" : "default"}
                    onClick={() => onOpen(event.cityId, event.cityStatus)}
                >
                    Open city
                </Button>
            </div>

            <p className="dashboard-event__detail">{event.detail}</p>
        </article>
    );
}

type WatchlistProps = {
    title: string;
    subtitle: string;
    cities: CityListItemView[];
    emptyText: string;
    actionLabel: string;
    onOpen: (city: CityListItemView) => void;
};

function WatchlistSection({title, subtitle, cities, emptyText, actionLabel, onOpen}: WatchlistProps) {
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
                    <div className="dashboard-empty-state__text">{emptyText}</div>
                </div>
            ) : (
                <div className="dashboard-watchlist">
                    {cities.map((city) => {
                        const statusTone = getCityStatusTone(city.status, city.archivedAtUtc);

                        return (
                            <article key={city.cityId}
                                     className={`dashboard-watch-item dashboard-watch-item--${statusTone}`}>
                                <div className="dashboard-watch-item__main">
                                    <div className="dashboard-watch-item__topline">
                                        <span className={`cities-status-pill cities-status-pill--${statusTone}`}>
                                            {formatCityStatusLabel(city.status, city.archivedAtUtc)}
                                        </span>
                                        <span className="dashboard-watch-item__id" title={city.cityId}>
                                            {formatCityShortId(city.cityId)}
                                        </span>
                                    </div>

                                    <h3 className="dashboard-watch-item__title">{city.name}</h3>
                                    <div className="dashboard-watch-item__meta">
                                        <span>{formatSimulationKindLabel(city.simulationKind)}</span>
                                        <span className="dashboard-watch-item__separator">/</span>
                                        <span>{describeCityLifecycle(city.status, city.archivedAtUtc, "registry")}</span>
                                    </div>
                                </div>

                                <div className="dashboard-watch-item__actions">
                                    <Button
                                        size="sm"
                                        variant={statusTone === "failed" ? "danger" : "primary"}
                                        onClick={() => onOpen(city)}
                                    >
                                        {actionLabel}
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

function DashboardContent({
                              dashboard,
                              canCreateCity,
                              isRefreshing,
                              onOpenCity,
                              onRefresh,
                          }: {
    dashboard: CityOperationsDashboardView;
    canCreateCity: boolean;
    isRefreshing: boolean;
    onOpenCity: (city: CityListItemView) => void;
    onRefresh: () => void;
}) {
    const metrics = [
        dashboard.trackedHosts,
        dashboard.readyHosts,
        dashboard.archivedRecords,
        dashboard.attentionQueue,
    ];
    const activityRows = [
        dashboard.newCities,
        dashboard.archivedCities,
        dashboard.failedBootstraps,
        dashboard.readyHandOffs,
    ];

    return (
        <>
            <header className="dashboard-header">
                <div className="dashboard-header__content">
                    <div className="dashboard-header__eyebrow">CityCore / Overview</div>
                    <h1 className="dashboard-header__title">Operations dashboard</h1>
                    <p className="dashboard-header__subtitle">
                        Monitor city throughput, compare current flow against yesterday, month, and year windows,
                        and keep service health plus the latest lifecycle events visible without diving into each host
                        first.
                    </p>
                    <div className="dashboard-header__meta">
                        <span className="settings-pill">Updated {formatDateTime(dashboard.generatedAtUtc)}</span>
                        <span className="settings-pill">{dashboard.trackedHosts.current} total records</span>
                    </div>
                </div>

                <div className="dashboard-header__actions">
                    {canCreateCity ? (
                        <Link className="cities-page__header-link cities-page__header-link--primary"
                              to={getClassicCitySetupPath()}>
                            Compose Classic City
                        </Link>
                    ) : null}

                    <Link className="cities-page__header-link" to={CLASSIC_CITY_LIST_PATH}>
                        Open registry
                    </Link>

                    <Button type="button" variant="default" onClick={onRefresh} disabled={isRefreshing}>
                        {isRefreshing ? "Refreshing..." : "Refresh dashboard"}
                    </Button>
                </div>
            </header>

            <section className="dashboard-metric-grid" aria-label="Operations dashboard metrics">
                {metrics.map((metric) => (
                    <MetricCard key={metric.label} metric={metric}/>
                ))}
            </section>

            <div className="dashboard-main-grid">
                <section className="dashboard-panel dashboard-panel--wide">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Operational deltas</h2>
                            <p className="dashboard-panel__subtitle">
                                Compare today, this month, and this year against the previous equivalent windows.
                            </p>
                        </div>
                    </div>

                    <div className="dashboard-activity-list">
                        {activityRows.map((row) => (
                            <ActivityRow key={row.label} row={row}/>
                        ))}
                    </div>
                </section>

                <section className="dashboard-panel">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">System health</h2>
                            <p className="dashboard-panel__subtitle">
                                Ready-state signals for the services this operator dashboard depends on most.
                            </p>
                        </div>
                    </div>

                    <div className="dashboard-health-list">
                        {dashboard.services.map((service) => (
                            <HealthItem key={service.service} service={service}/>
                        ))}
                    </div>
                </section>

                <section className="dashboard-panel dashboard-panel--wide">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Recent operator events</h2>
                            <p className="dashboard-panel__subtitle">
                                Latest city lifecycle signals worth noticing before switching into a specific workspace.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.events.length}</span>
                    </div>

                    <div className="dashboard-event-list">
                        {dashboard.events.map((event) => (
                            <EventItem
                                key={`${event.kind}-${event.cityId}-${event.occurredAtUtc}`}
                                event={event}
                                onOpen={(cityId, status) => {
                                    onOpenCity({
                                        cityId,
                                        simulationId: cityId,
                                        name: event.cityName,
                                        simulationKind: "ClassicCity",
                                        status,
                                        createdAtUtc: event.occurredAtUtc,
                                    });
                                }}
                            />
                        ))}
                    </div>
                </section>

                <WatchlistSection
                    title="Attention queue"
                    subtitle="Cities still stuck in provisioning or already marked as failed."
                    cities={dashboard.attentionCities}
                    emptyText="No provisioning handoff currently needs attention."
                    actionLabel="Open handoff"
                    onOpen={onOpenCity}
                />

                <WatchlistSection
                    title="Ready monitoring"
                    subtitle="Most recently ready city hosts that can be opened directly for live inspection."
                    cities={dashboard.readyCities}
                    emptyText="No city has completed provisioning yet."
                    actionLabel="Open monitoring"
                    onOpen={onOpenCity}
                />

                <WatchlistSection
                    title="Archived records"
                    subtitle="Recently archived cities that remain available for audit and cleanup review."
                    cities={dashboard.archivedCitiesList}
                    emptyText="Archived records will appear here once cities are taken out of active monitoring."
                    actionLabel="Review record"
                    onOpen={onOpenCity}
                />
            </div>
        </>
    );
}

const DashboardPage = () => {
    const navigate = useNavigate();
    const {can} = usePermissions();
    const canCreateCity = can(PermissionKeys.CityCoreClassicCityCreate);
    const dashboardQuery = useCityOperationsDashboardQuery();

    useEffect(() => {
        const intervalId = window.setInterval(() => {
            if (document.visibilityState !== "visible") {
                return;
            }

            void dashboardQuery.refetch();
        }, DASHBOARD_AUTO_REFRESH_MS);

        return () => {
            window.clearInterval(intervalId);
        };
    }, [dashboardQuery.refetch]);

    const openCity = (city: CityListItemView) => {
        navigate(openCityPath(city.cityId, city.status, city.archivedAtUtc));
    };

    return (
        <section className="dashboard-page">
            {(dashboardQuery.error && !dashboardQuery.data) ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{dashboardQuery.error}</span>
                    <Button type="button" variant="primary" onClick={() => void dashboardQuery.refetch()}>
                        Retry
                    </Button>
                </div>
            ) : null}

            {dashboardQuery.data ? (
                <DashboardContent
                    dashboard={dashboardQuery.data}
                    canCreateCity={canCreateCity}
                    isRefreshing={dashboardQuery.isLoading}
                    onOpenCity={openCity}
                    onRefresh={() => void dashboardQuery.refetch()}
                />
            ) : (
                <section className="dashboard-loading" role="status" aria-live="polite">
                    <div className="dashboard-loading__title">Loading operations dashboard</div>
                    <div className="dashboard-loading__text">
                        Pulling real city metrics, system health, and lifecycle activity into the watchboard.
                    </div>
                </section>
            )}
        </section>
    );
};

export default DashboardPage;
