import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import {useCityDashboard} from "@services/citycore/scenarios/classic-city/hooks/useCityDashboard";
import type {
    CityDashboardActivityEventView,
    CityDashboardMetricView,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

function formatNumber(value: number, valueKind: string) {
    return new Intl.NumberFormat(document.documentElement.lang || undefined, {
        maximumFractionDigits: valueKind === "average" ? 2 : 0,
        minimumFractionDigits: 0,
    }).format(value);
}

function formatDelta(value: number | null | undefined, valueKind: string) {
    if (typeof value !== "number" || Number.isNaN(value)) {
        return "--";
    }

    const formatted = formatNumber(Math.abs(value), valueKind);
    if (value === 0) {
        return "0";
    }

    return value > 0
        ? `+${formatted}`
        : `-${formatted}`;
}

function getDeltaTone(value: number | null | undefined) {
    if (typeof value !== "number" || value === 0) {
        return "neutral";
    }

    return value > 0 ? "positive" : "negative";
}

function formatDateTime(value: string | null | undefined) {
    if (!value) {
        return "--";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return date.toLocaleString();
}

function humanize(value: string) {
    return value
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .trim()
        .replace(/\b\w/g, (match) => match.toUpperCase());
}

function MetricCard({metric}: { metric: CityDashboardMetricView }) {
    return (
        <article className="city-dashboard-metric-card">
            <div className="city-dashboard-metric-card__topline">
                <span className="city-dashboard-metric-card__label">{metric.label}</span>
                <span className="city-dashboard-metric-card__value">
                    {formatNumber(metric.currentValue, metric.valueKind)}
                </span>
            </div>
            <p className="city-dashboard-metric-card__description">{metric.description}</p>

            <div className="city-dashboard-metric-card__delta-grid">
                <div className="city-dashboard-metric-card__delta">
                    <span className="city-dashboard-metric-card__delta-label">Day</span>
                    <span className={`city-dashboard-metric-card__delta-value city-dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaYesterday)}`}>
                        {formatDelta(metric.deltaYesterday, metric.valueKind)}
                    </span>
                </div>
                <div className="city-dashboard-metric-card__delta">
                    <span className="city-dashboard-metric-card__delta-label">Month</span>
                    <span className={`city-dashboard-metric-card__delta-value city-dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaMonth)}`}>
                        {formatDelta(metric.deltaMonth, metric.valueKind)}
                    </span>
                </div>
                <div className="city-dashboard-metric-card__delta">
                    <span className="city-dashboard-metric-card__delta-label">Year</span>
                    <span className={`city-dashboard-metric-card__delta-value city-dashboard-metric-card__delta-value--${getDeltaTone(metric.deltaYear)}`}>
                        {formatDelta(metric.deltaYear, metric.valueKind)}
                    </span>
                </div>
            </div>
        </article>
    );
}

function ActivityItem({event}: { event: CityDashboardActivityEventView }) {
    return (
        <article className={`city-dashboard-event city-dashboard-event--${event.severity.toLowerCase()}`}>
            <div className="city-dashboard-event__topline">
                <div className="city-dashboard-event__heading">
                    <h3 className="city-dashboard-event__title">{event.title}</h3>
                    <div className="city-dashboard-event__meta">
                        <span>{formatDateTime(event.occurredAtUtc)}</span>
                        <span className="city-dashboard-event__separator">/</span>
                        <span>Sim date {event.currentDate}</span>
                    </div>
                </div>

                <div className="city-dashboard-event__pills">
                    <span className={`city-dashboard-event__pill city-dashboard-event__pill--${event.source.toLowerCase()}`}>
                        {humanize(event.source)}
                    </span>
                    <span className={`city-dashboard-event__pill city-dashboard-event__pill--${event.severity.toLowerCase()}`}>
                        {humanize(event.severity)}
                    </span>
                </div>
            </div>

            <p className="city-dashboard-event__summary">{event.summary}</p>
        </article>
    );
}

export function CityDashboardCard({
                                      cityId,
                                      cityName,
                                      isArchived = false,
                                  }: Props) {
    const dashboardQuery = useCityDashboard(cityId, isArchived ? 0 : 30000);
    const dashboard = dashboardQuery.data;

    return (
        <Card
            title="Dashboard"
            subtitle="City-scale metrics with recent life events, compared against the previous simulation day, month, and year."
            right={(
                <Button
                    size="sm"
                    onClick={() => {
                        void dashboardQuery.refetch();
                    }}
                    disabled={dashboardQuery.isLoading}
                >
                    {dashboardQuery.isRefreshing ? "Refreshing..." : dashboardQuery.isLoading ? "Loading..." : "Refresh"}
                </Button>
            )}
        >
            {dashboardQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{dashboardQuery.error}</span>
                </div>
            ) : null}

            {dashboardQuery.isLoading && !dashboard ? (
                <div className="city-dashboard-loading" role="status" aria-live="polite">
                    <div className="city-dashboard-loading__title">Loading city dashboard</div>
                    <div className="city-dashboard-loading__text">
                        Pulling local simulation metrics and the most recent resident activity into the city workspace.
                    </div>
                </div>
            ) : null}

            {!dashboard && dashboardQuery.isUnavailable ? (
                <div className="city-dashboard-empty" role="status">
                    <div className="city-dashboard-empty__title">Dashboard is not available yet</div>
                    <div className="city-dashboard-empty__text">
                        {cityName
                            ? `The city "${cityName}" does not have a population-backed dashboard snapshot yet.`
                            : "This city does not have a population-backed dashboard snapshot yet."}
                    </div>
                </div>
            ) : null}

            {dashboard ? (
                <div className="city-dashboard">
                    <section className="city-dashboard-hero">
                        <div className="city-dashboard-hero__content">
                            <div className="city-dashboard-hero__eyebrow">Classic City dashboard</div>
                            <div className="city-dashboard-hero__title-row">
                                <h3 className="city-dashboard-hero__title">
                                    Sim date {dashboard.currentDate}
                                </h3>
                                <span className="city-dashboard-hero__badge">
                                    {isArchived ? "Archived snapshot" : "Live city view"}
                                </span>
                            </div>
                            <p className="city-dashboard-hero__summary">
                                Metrics are compared against the previous simulation day, month, and year instead of real-world time.
                            </p>
                        </div>

                        <div className="city-dashboard-hero__aside">
                            <span className="city-dashboard-hero__aside-label">Updated</span>
                            <strong className="city-dashboard-hero__aside-value">
                                {formatDateTime(dashboard.generatedAtUtc)}
                            </strong>
                        </div>
                    </section>

                    <section className="city-dashboard-metric-grid" aria-label="Classic City dashboard metrics">
                        {dashboard.metrics.map((metric) => (
                            <MetricCard key={metric.key} metric={metric}/>
                        ))}
                    </section>

                    <section className="city-dashboard-events-panel">
                        <div className="city-dashboard-events-panel__header">
                            <div>
                                <h3 className="city-dashboard-events-panel__title">Recent city activity</h3>
                                <p className="city-dashboard-events-panel__subtitle">
                                    Latest resident and household changes currently visible from the population layer.
                                </p>
                            </div>
                            <span className="city-dashboard-events-panel__count">{dashboard.recentEvents.length}</span>
                        </div>

                        {dashboard.recentEvents.length === 0 ? (
                            <div className="city-dashboard-empty city-dashboard-empty--inline" role="status">
                                <div className="city-dashboard-empty__text">
                                    Activity entries will appear here once operator or simulation-driven city events are recorded.
                                </div>
                            </div>
                        ) : (
                            <div className="city-dashboard-event-list">
                                {dashboard.recentEvents.map((event) => (
                                    <ActivityItem key={event.activityEventId} event={event}/>
                                ))}
                            </div>
                        )}
                    </section>
                </div>
            ) : null}
        </Card>
    );
}
