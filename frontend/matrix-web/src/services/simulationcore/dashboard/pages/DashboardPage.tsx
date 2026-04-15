import {useEffect} from "react";
import {Link, useNavigate} from "react-router-dom";
import type {
    DashboardBudgetPressureView,
    DashboardEnvironmentalAlertView,
    CityOperationsDashboardView,
    DashboardDistrictResponsePriorityView,
    DashboardMetricView,
    DashboardMobilityView,
    DashboardPeriodComparisonRowView,
    DashboardPhaseProgressView,
    DashboardPopulationDistrictPressureView,
    DashboardRecentEventView,
    DashboardServiceHealthView,
    DashboardTickFreshnessView,
} from "@services/simulationcore/dashboard/api/dashboardTypes";
import {useCityOperationsDashboardQuery} from "@services/simulationcore/dashboard/hooks/useCityOperationsDashboardQuery";
import type {CityListItemView} from "@services/simulationcore/scenarios/classic-city/contracts/citiesContracts";
import {
    describeCityLifecycle,
    formatCityShortId,
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/simulationcore/scenarios/classic-city/utils/presentation";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCitySetupPath,
} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/simulationcore/scenarios/classic-city/styles/cities.css";
import "@services/simulationcore/dashboard/styles/dashboard.css";

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

function formatIndex(value: number | null | undefined) {
    if (typeof value !== "number" || Number.isNaN(value)) {
        return "--";
    }

    return `${Math.round(value * 100)}%`;
}

function formatMinutes(value: number | null | undefined) {
    if (typeof value !== "number" || Number.isNaN(value)) {
        return "--";
    }

    if (value < 60) {
        return `${Math.round(value)} min`;
    }

    const hours = value / 60;
    return `${hours.toFixed(hours >= 10 ? 0 : 1)} h`;
}

function formatCompactId(value: string) {
    return value.length > 8 ? value.slice(0, 8) : value;
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

function getSeverityTone(value: string) {
    switch (value.trim().toLowerCase()) {
        case "critical":
        case "danger":
        case "high":
            return "danger";
        case "warning":
        case "elevated":
        case "medium":
            return "warning";
        default:
            return "success";
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

type MetricSectionProps = {
    title: string;
    subtitle: string;
    metrics: DashboardMetricView[];
};

function MetricSection({title, subtitle, metrics}: MetricSectionProps) {
    return (
        <section className="dashboard-panel">
            <div className="dashboard-panel__header">
                <div>
                    <h2 className="dashboard-panel__title">{title}</h2>
                    <p className="dashboard-panel__subtitle">{subtitle}</p>
                </div>
            </div>

            <div className="dashboard-metric-grid">
                {metrics.map((metric) => (
                    <MetricCard key={metric.label} metric={metric}/>
                ))}
            </div>
        </section>
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

function DistrictPriorityItem({
                                  item,
                                  onOpen,
                              }: {
    item: DashboardDistrictResponsePriorityView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>District {formatCompactId(item.districtId)}</span>
                        <span className="dashboard-watch-item__separator">/</span>
                        <span>{item.recommendedFocus}</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Priority</span>
                    <strong>{formatIndex(item.priorityScore)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Social pressure</span>
                    <strong>{formatIndex(item.populationPressureIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Service disruption</span>
                    <strong>{formatIndex(item.serviceDisruptionIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Severe illness</span>
                    <strong>{item.severeIllnessCount}</strong>
                </div>
            </div>
        </article>
    );
}

function MobilityItem({
                          item,
                          onOpen,
                      }: {
    item: DashboardMobilityView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>{item.activeTripCount} active trips</span>
                        <span className="dashboard-watch-item__separator">/</span>
                        <span>{formatMinutes(item.averageRemainingTravelMinutes)} avg remaining</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Mobility pressure</span>
                    <strong>{formatIndex(item.mobilityPressureIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Delayed</span>
                    <strong>{item.delayedTripCount}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Dynamic roads</span>
                    <strong>{item.dynamicRoadTripCount}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Slowdown</span>
                    <strong>{item.averageSlowdownRatio.toFixed(2)}x</strong>
                </div>
            </div>

            {item.trips.length > 0 ? (
                <div className="dashboard-trip-list">
                    {item.trips.slice(0, 3).map((trip) => (
                        <div key={trip.tripId} className="dashboard-trip-token">
                            <span>{trip.subject}</span>
                            <span>{formatIndex(trip.currentProgressIndex)}</span>
                        </div>
                    ))}
                </div>
            ) : null}
        </article>
    );
}

function EnvironmentalAlertItem({
                                    item,
                                    onOpen,
                                }: {
    item: DashboardEnvironmentalAlertView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>{item.severity}</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Alert score</span>
                    <strong>{formatIndex(item.alertScore)}</strong>
                </div>
            </div>
        </article>
    );
}

function PopulationPressureItem({
                                    item,
                                    onOpen,
                                }: {
    item: DashboardPopulationDistrictPressureView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>District {formatCompactId(item.districtId)}</span>
                        <span className="dashboard-watch-item__separator">/</span>
                        <span>{item.residentCount} residents</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Population pressure</span>
                    <strong>{formatIndex(item.populationPressureIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Utility continuity</span>
                    <strong>{formatIndex(item.utilityContinuityIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Housing fragility</span>
                    <strong>{formatIndex(item.housingFragilityIndex)}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Homeless</span>
                    <strong>{item.homelessResidentCount}</strong>
                </div>
            </div>
        </article>
    );
}

function BudgetPressureItem({
                                item,
                                onOpen,
                            }: {
    item: DashboardBudgetPressureView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);
    const categories = [
        item.controls.general,
        item.controls.operations,
        item.controls.infrastructure,
        item.controls.healthcare,
    ];

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>{item.controlStatus}</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Pressure</span>
                    <strong>{formatIndex(item.pressureIndex)}</strong>
                </div>
            </div>

            <div className="dashboard-budget-grid">
                {categories.map((category) => (
                    <div key={category.category} className="dashboard-budget-token">
                        <span>{category.category}</span>
                        <strong>{category.authorizationLevel}</strong>
                    </div>
                ))}
            </div>
        </article>
    );
}

function TickFreshnessItem({
                               item,
                               onOpen,
                           }: {
    item: DashboardTickFreshnessView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>Tick skew {item.tickSkew}</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Environmental tick</span>
                    <strong>{item.environmentalTickId}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Budget tick</span>
                    <strong>{item.budgetTickId}</strong>
                </div>
            </div>
        </article>
    );
}

function PhaseProgressItem({
                               item,
                               onOpen,
                           }: {
    item: DashboardPhaseProgressView;
    onOpen: (cityId: string, status: string) => void;
}) {
    const tone = getSeverityTone(item.severity);

    return (
        <article className={`dashboard-signal-item dashboard-signal-item--${tone}`}>
            <div className="dashboard-signal-item__topline">
                <div>
                    <h3 className="dashboard-signal-item__title">{item.cityName}</h3>
                    <div className="dashboard-signal-item__meta">
                        <span>{item.laggingService} lagging</span>
                        <span className="dashboard-watch-item__separator">/</span>
                        <span>{item.leadingService} leading</span>
                    </div>
                </div>

                <Button size="sm" variant={tone === "danger" ? "danger" : "default"}
                        onClick={() => onOpen(item.cityId, item.cityStatus)}>
                    Open city
                </Button>
            </div>

            <p className="dashboard-signal-item__summary">{item.summary}</p>

            <div className="dashboard-signal-item__stats">
                <div>
                    <span className="dashboard-signal-item__stat-label">Systems</span>
                    <strong>{item.systemsPhase}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Resources</span>
                    <strong>{item.resourcesPhase}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Budget</span>
                    <strong>{item.budgetPhase}</strong>
                </div>
                <div>
                    <span className="dashboard-signal-item__stat-label">Tick spread</span>
                    <strong>{item.tickSpread}</strong>
                </div>
            </div>
        </article>
    );
}

function DashboardContent({
                              dashboard,
                              canCreateCity,
                              isRefreshing,
                              onOpenCity,
                              onOpenCityByMeta,
                              onRefresh,
                          }: {
    dashboard: CityOperationsDashboardView;
    canCreateCity: boolean;
    isRefreshing: boolean;
    onOpenCity: (city: CityListItemView) => void;
    onOpenCityByMeta: (cityId: string, status: string, archivedAtUtc?: string | null) => void;
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
    const alertMetrics = [
        dashboard.environmentalAlerts,
        dashboard.populationDistrictAlerts,
        dashboard.districtResponsePriorityAlerts,
        dashboard.mobilityAlerts,
        dashboard.operationalBudgetAlerts,
        dashboard.tickFreshnessAlerts,
        dashboard.phaseProgressAlerts,
    ];

    return (
        <>
            <header className="dashboard-header">
                <div className="dashboard-header__content">
                    <div className="dashboard-header__eyebrow">SimulationCore / Overview</div>
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

            <MetricSection
                title="Operational alerts"
                subtitle="The strongest live operator signals already aggregated in the gateway from district, mobility, budget, and runtime drift layers."
                metrics={alertMetrics}
            />

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

                <section className="dashboard-panel dashboard-panel--wide">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">District response priorities</h2>
                            <p className="dashboard-panel__subtitle">
                                District-level focus recommendations built from social pressure, utility instability,
                                and service disruption.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.districtResponsePriorities.length}</span>
                    </div>

                    {dashboard.districtResponsePriorities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No district-level response targets are currently ranked above the alert threshold.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.districtResponsePriorities.map((item) => (
                                <DistrictPriorityItem
                                    key={`${item.cityId}-${item.districtId}`}
                                    item={item}
                                    onOpen={onOpenCityByMeta}
                                />
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Mobility pressure</h2>
                            <p className="dashboard-panel__subtitle">
                                Active commute and healthcare trips that are getting slower or bunching up under live
                                road conditions.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.mobilityCities.length}</span>
                    </div>

                    {dashboard.mobilityCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No cities are currently showing meaningful world-mobility pressure.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.mobilityCities.map((item) => (
                                <MobilityItem key={item.cityId} item={item} onOpen={onOpenCityByMeta}/>
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel dashboard-panel--wide">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Environmental fronts</h2>
                            <p className="dashboard-panel__subtitle">
                                Cities where environmental and utility conditions are currently pushing the highest alert score.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.environmentalCities.length}</span>
                    </div>

                    {dashboard.environmentalCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No cities are currently crossing the environmental alert threshold.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.environmentalCities.map((item) => (
                                <EnvironmentalAlertItem key={item.cityId} item={item} onOpen={onOpenCityByMeta}/>
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">District social pressure</h2>
                            <p className="dashboard-panel__subtitle">
                                The hardest-hit districts by population wellbeing, illness load, housing fragility, and local utility continuity.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.populationDistrictCities.length}</span>
                    </div>

                    {dashboard.populationDistrictCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No district-level social pressure is currently breaching the watch threshold.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.populationDistrictCities.map((item) => (
                                <PopulationPressureItem
                                    key={`${item.cityId}-${item.districtId}`}
                                    item={item}
                                    onOpen={onOpenCityByMeta}
                                />
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel dashboard-panel--wide">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Budget pressure</h2>
                            <p className="dashboard-panel__subtitle">
                                Operational budget caps that are actively tightening city response capacity.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.budgetPressureCities.length}</span>
                    </div>

                    {dashboard.budgetPressureCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No cities are currently breaching the budget pressure watch threshold.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.budgetPressureCities.map((item) => (
                                <BudgetPressureItem key={item.cityId} item={item} onOpen={onOpenCityByMeta}/>
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Tick freshness drift</h2>
                            <p className="dashboard-panel__subtitle">
                                Cities where runtime snapshots are no longer lining up on the same effective tick.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.tickFreshnessCities.length}</span>
                    </div>

                    {dashboard.tickFreshnessCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No cross-service tick freshness drift is currently visible.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.tickFreshnessCities.map((item) => (
                                <TickFreshnessItem key={item.cityId} item={item} onOpen={onOpenCityByMeta}/>
                            ))}
                        </div>
                    )}
                </section>

                <section className="dashboard-panel">
                    <div className="dashboard-panel__header">
                        <div>
                            <h2 className="dashboard-panel__title">Phase progression</h2>
                            <p className="dashboard-panel__subtitle">
                                Phase discipline mismatches between systems, resources, and budget progression.
                            </p>
                        </div>
                        <span className="settings-pill">{dashboard.phaseProgressCities.length}</span>
                    </div>

                    {dashboard.phaseProgressCities.length === 0 ? (
                        <div className="dashboard-empty-state" role="status">
                            <div className="dashboard-empty-state__text">
                                No phase progression mismatches are currently visible.
                            </div>
                        </div>
                    ) : (
                        <div className="dashboard-signal-list">
                            {dashboard.phaseProgressCities.map((item) => (
                                <PhaseProgressItem key={item.cityId} item={item} onOpen={onOpenCityByMeta}/>
                            ))}
                        </div>
                    )}
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
    const canCreateCity = can(PermissionKeys.SimulationCoreClassicCityCreate);
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

    const openCityByMeta = (cityId: string, status: string, archivedAtUtc?: string | null) => {
        navigate(openCityPath(cityId, status, archivedAtUtc));
    };

    return (
        <section className="dashboard-page">
            {(dashboardQuery.error && !dashboardQuery.data) ? (
                <div className="simulationcore-error-banner" role="alert">
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
                    onOpenCityByMeta={openCityByMeta}
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
