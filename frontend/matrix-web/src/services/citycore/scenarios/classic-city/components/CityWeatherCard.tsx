import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import {useCityWeather} from "@services/citycore/scenarios/classic-city/hooks/useCityWeather";
import "@services/citycore/scenarios/classic-city/styles/city-weather.css";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

function humanize(value: string | null | undefined): string {
    if (!value) {
        return "--";
    }

    return value
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .replace(/\s+/g, " ")
        .trim()
        .replace(/\b\w/g, (match) => match.toUpperCase());
}

function formatDateTime(value: string | null | undefined): string {
    if (!value) {
        return "--";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return date.toLocaleString();
}

function formatRelativeTime(value: string | null | undefined): string {
    if (!value) {
        return "--";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return value;
    }

    const diffMinutes = Math.round((date.getTime() - Date.now()) / 60000);

    if (Math.abs(diffMinutes) < 1) {
        return "Just now";
    }

    const formatter = new Intl.RelativeTimeFormat("en", {numeric: "auto"});

    if (Math.abs(diffMinutes) < 60) {
        return formatter.format(diffMinutes, "minute");
    }

    const diffHours = Math.round(diffMinutes / 60);

    if (Math.abs(diffHours) < 24) {
        return formatter.format(diffHours, "hour");
    }

    const diffDays = Math.round(diffHours / 24);
    return formatter.format(diffDays, "day");
}

function formatNumber(value: number, maximumFractionDigits = 1): string {
    return new Intl.NumberFormat(undefined, {
        maximumFractionDigits,
        minimumFractionDigits: 0,
    }).format(value);
}

function formatUtcOffset(minutes: number | undefined): string {
    if (typeof minutes !== "number" || !Number.isFinite(minutes)) {
        return "--";
    }

    const sign = minutes >= 0 ? "+" : "-";
    const absoluteMinutes = Math.abs(minutes);
    const hours = Math.floor(absoluteMinutes / 60)
        .toString()
        .padStart(2, "0");
    const restMinutes = (absoluteMinutes % 60).toString().padStart(2, "0");

    return `UTC${sign}${hours}:${restMinutes}`;
}

export function CityWeatherCard({
                                    cityId,
                                    cityName,
                                    isArchived = false,
                                }: Props) {
    const weatherQuery = useCityWeather(cityId, isArchived ? 0 : 15000);
    const weather = weatherQuery.data;
    const weatherModeClass = weather?.activeOverride
        ? "city-weather-badge--override"
        : isArchived
            ? "city-weather-badge--snapshot"
            : "city-weather-badge--live";
    const weatherModeLabel = weather?.activeOverride
        ? "Override active"
        : isArchived
            ? "Snapshot"
            : "Live weather";

    return (
        <Card
            title="Weather"
            subtitle="Current atmospheric state, climate context, and weather timing."
            right={
                <Button
                    size="sm"
                    onClick={() => {
                        void weatherQuery.refetch();
                    }}
                    disabled={weatherQuery.isLoading}
                >
                    {weatherQuery.isRefreshing ? "Refreshing..." : weatherQuery.isLoading ? "Loading..." : "Refresh"}
                </Button>
            }
        >
            {weatherQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{weatherQuery.error}</span>
                </div>
            ) : null}

            {weatherQuery.isLoading && !weather ? (
                <div className="city-weather-skeleton" aria-hidden="true">
                    <div className="city-weather-skeleton__hero"/>
                    <div className="city-weather-skeleton__grid">
                        <div className="city-weather-skeleton__tile"/>
                        <div className="city-weather-skeleton__tile"/>
                        <div className="city-weather-skeleton__tile"/>
                        <div className="city-weather-skeleton__tile"/>
                    </div>
                </div>
            ) : null}

            {!weather && weatherQuery.isUnavailable ? (
                <div className="city-weather-empty" role="status">
                    <div className="city-weather-empty__title">
                        Weather state is not available yet
                    </div>
                    <div className="city-weather-empty__text">
                        {cityName
                            ? `The city "${cityName}" does not have a weather snapshot yet.`
                            : "This city does not have a weather snapshot yet."}
                    </div>
                </div>
            ) : null}

            {weather ? (
                <div className="city-weather">
                    <section className="city-weather-hero">
                        <div className="city-weather-hero__content">
                            <div className="city-weather-hero__eyebrow">Current weather</div>

                            <div className="city-weather-hero__title-row">
                                <h3 className="city-weather-hero__title">
                                    {humanize(weather.currentType)}
                                </h3>
                                <span className={`city-weather-badge ${weatherModeClass}`}>
                                    {weatherModeLabel}
                                </span>
                            </div>

                            <div className="city-weather-hero__summary">
                                {humanize(weather.severity)} severity
                                <span className="city-weather-hero__separator">/</span>
                                {humanize(weather.precipitationKind)}
                            </div>
                        </div>

                        <div className="city-weather-hero__temperature">
                            <span className="city-weather-hero__temperature-value">
                                {formatNumber(weather.temperatureC)} C
                            </span>
                            <span className="city-weather-hero__temperature-caption">
                                Air temperature
                            </span>
                        </div>
                    </section>

                    <section className="city-weather-metrics">
                        <article className="city-weather-metric">
                            <div className="city-weather-metric__label">Humidity</div>
                            <div className="city-weather-metric__value">
                                {formatNumber(weather.humidityPercent)}%
                            </div>
                        </article>

                        <article className="city-weather-metric">
                            <div className="city-weather-metric__label">Wind</div>
                            <div className="city-weather-metric__value">
                                {formatNumber(weather.windSpeedKph)} km/h
                            </div>
                        </article>

                        <article className="city-weather-metric">
                            <div className="city-weather-metric__label">Cloud cover</div>
                            <div className="city-weather-metric__value">
                                {formatNumber(weather.cloudCoveragePercent)}%
                            </div>
                        </article>

                        <article className="city-weather-metric">
                            <div className="city-weather-metric__label">Pressure</div>
                            <div className="city-weather-metric__value">
                                {formatNumber(weather.pressureHpa)} hPa
                            </div>
                        </article>
                    </section>

                    <section className="city-weather-facts">
                        <span className="city-weather-fact-chip">
                            Climate zone: {humanize(weather.climateZone)}
                        </span>
                        <span className="city-weather-fact-chip">
                            Hemisphere: {humanize(weather.hemisphere)}
                        </span>
                        <span className="city-weather-fact-chip">
                            Offset: {formatUtcOffset(weather.utcOffsetMinutes)}
                        </span>
                    </section>

                    <section className="city-weather-timeline">
                        <article className="city-weather-timeline__item">
                            <div className="city-weather-timeline__label">Started</div>
                            <div className="city-weather-timeline__value">
                                {formatRelativeTime(weather.startedAtUtc)}
                            </div>
                            <div className="city-weather-timeline__meta">
                                {formatDateTime(weather.startedAtUtc)}
                            </div>
                        </article>

                        <article className="city-weather-timeline__item">
                            <div className="city-weather-timeline__label">Expected until</div>
                            <div className="city-weather-timeline__value">
                                {formatRelativeTime(weather.expectedUntilUtc)}
                            </div>
                            <div className="city-weather-timeline__meta">
                                {formatDateTime(weather.expectedUntilUtc)}
                            </div>
                        </article>

                        <article className="city-weather-timeline__item">
                            <div className="city-weather-timeline__label">Last evaluated</div>
                            <div className="city-weather-timeline__value">
                                {formatRelativeTime(weather.lastEvaluatedAtUtc)}
                            </div>
                            <div className="city-weather-timeline__meta">
                                {formatDateTime(weather.lastEvaluatedAtUtc)}
                            </div>
                        </article>

                        <article className="city-weather-timeline__item">
                            <div className="city-weather-timeline__label">Last transition</div>
                            <div className="city-weather-timeline__value">
                                {formatRelativeTime(weather.lastTransitionAtUtc)}
                            </div>
                            <div className="city-weather-timeline__meta">
                                {formatDateTime(weather.lastTransitionAtUtc)}
                            </div>
                        </article>
                    </section>

                    {weather.activeOverride ? (
                        <section className="city-weather-override">
                            <div className="city-weather-override__title">Manual override</div>
                            <div className="city-weather-override__text">
                                {humanize(weather.activeOverride.forcedType)}
                                <span className="city-weather-hero__separator">/</span>
                                {humanize(weather.activeOverride.forcedSeverity)}
                                <span className="city-weather-hero__separator">/</span>
                                {humanize(weather.activeOverride.forcedPrecipitationKind)}
                            </div>

                            <div className="city-weather-facts">
                                <span className="city-weather-fact-chip">
                                    Source: {humanize(weather.activeOverride.source)}
                                </span>
                                <span className="city-weather-fact-chip">
                                    Starts: {formatDateTime(weather.activeOverride.startsAtUtc)}
                                </span>
                                <span className="city-weather-fact-chip">
                                    Ends: {formatDateTime(weather.activeOverride.endsAtUtc)}
                                </span>
                                {weather.activeOverride.reason ? (
                                    <span className="city-weather-fact-chip">
                                        Reason: {weather.activeOverride.reason}
                                    </span>
                                ) : null}
                            </div>
                        </section>
                    ) : null}
                </div>
            ) : null}
        </Card>
    );
}
