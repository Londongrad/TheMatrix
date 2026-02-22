import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import {useCityPopulationSummary} from "@services/citycore/scenarios/classic-city/hooks/useCityPopulationSummary";
import "@services/citycore/scenarios/classic-city/styles/city-population-summary.css";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

function formatNumber(value: number | null | undefined, maximumFractionDigits = 0): string {
    if (typeof value !== "number" || !Number.isFinite(value)) {
        return "--";
    }

    return new Intl.NumberFormat(undefined, {
        maximumFractionDigits,
        minimumFractionDigits: 0,
    }).format(value);
}

function formatMetric(value: number | null | undefined): string {
    return formatNumber(value, 2);
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

export function CityPopulationSummaryCard({
                                              cityId,
                                              cityName,
                                              isArchived = false,
                                          }: Props) {
    const summaryQuery = useCityPopulationSummary(cityId, isArchived ? 0 : 15000);
    const summary = summaryQuery.data;
    const residents = summary?.residents;
    const housing = summary?.housing;

    return (
        <Card
            title="Population"
            subtitle="Residents, housing distribution, wellbeing, and demographic breakdown."
            right={
                <Button
                    size="sm"
                    onClick={() => {
                        void summaryQuery.refetch();
                    }}
                    disabled={summaryQuery.isLoading}
                >
                    {summaryQuery.isRefreshing ? "Refreshing..." : summaryQuery.isLoading ? "Loading..." : "Refresh"}
                </Button>
            }
        >
            {summaryQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{summaryQuery.error}</span>
                </div>
            ) : null}

            {summaryQuery.isLoading && !summary ? (
                <div className="city-population-skeleton" aria-hidden="true">
                    <div className="city-population-skeleton__hero"/>
                    <div className="city-population-skeleton__grid">
                        <div className="city-population-skeleton__tile"/>
                        <div className="city-population-skeleton__tile"/>
                        <div className="city-population-skeleton__tile"/>
                        <div className="city-population-skeleton__tile"/>
                    </div>
                </div>
            ) : null}

            {!summary && summaryQuery.isUnavailable ? (
                <div className="city-population-empty" role="status">
                    <div className="city-population-empty__title">
                        Population summary is not available yet
                    </div>
                    <div className="city-population-empty__text">
                        {cityName
                            ? `The city "${cityName}" does not have an initialized population snapshot yet.`
                            : "This city does not have an initialized population snapshot yet."}
                    </div>
                </div>
            ) : null}

            {summary && residents && housing ? (
                <div className="city-population">
                    <section className="city-population-hero">
                        <div className="city-population-hero__content">
                            <div className="city-population-hero__eyebrow">Current population</div>
                            <div className="city-population-hero__title-row">
                                <h3 className="city-population-hero__title">
                                    {formatNumber(residents.residentCount)} residents
                                </h3>
                                <span className="city-population-badge">
                                    {isArchived ? "Snapshot" : "Live summary"}
                                </span>
                            </div>
                            <div className="city-population-hero__summary">
                                Sim date {summary.currentDate}
                                {summary.simulation ? (
                                    <>
                                        <span className="city-population-hero__separator">/</span>
                                        Tick {formatNumber(summary.simulation.lastProcessedTickId)}
                                    </>
                                ) : null}
                            </div>
                        </div>

                        <div className="city-population-hero__aside">
                            <span className="city-population-hero__aside-label">Households</span>
                            <span className="city-population-hero__aside-value">
                                {formatNumber(housing.householdCount)}
                            </span>
                        </div>
                    </section>

                    <section className="city-population-metrics">
                        <article className="city-population-metric">
                            <div className="city-population-metric__label">Housed residents</div>
                            <div className="city-population-metric__value">
                                {formatNumber(residents.housedResidentCount)}
                            </div>
                        </article>

                        <article className="city-population-metric">
                            <div className="city-population-metric__label">Homeless residents</div>
                            <div className="city-population-metric__value">
                                {formatNumber(residents.homelessResidentCount)}
                            </div>
                        </article>

                        <article className="city-population-metric">
                            <div className="city-population-metric__label">Deceased</div>
                            <div className="city-population-metric__value">
                                {formatNumber(residents.deceasedCount)}
                            </div>
                        </article>

                        <article className="city-population-metric">
                            <div className="city-population-metric__label">Homeless households</div>
                            <div className="city-population-metric__value">
                                {formatNumber(housing.homelessHouseholdCount)}
                            </div>
                        </article>
                    </section>

                    <section className="city-population-grid">
                        <article className="city-population-panel">
                            <div className="city-population-panel__title">Wellbeing</div>
                            <div className="city-population-kv">
                                <span>Health</span>
                                <strong>{formatMetric(residents.averageHealth)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Happiness</span>
                                <strong>{formatMetric(residents.averageHappiness)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Energy</span>
                                <strong>{formatMetric(residents.averageEnergy)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Stress</span>
                                <strong>{formatMetric(residents.averageStress)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Social need</span>
                                <strong>{formatMetric(residents.averageSocialNeed)}</strong>
                            </div>
                        </article>

                        <article className="city-population-panel">
                            <div className="city-population-panel__title">Age mix</div>
                            <div className="city-population-kv">
                                <span>Children</span>
                                <strong>{formatNumber(residents.childCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Youth</span>
                                <strong>{formatNumber(residents.youthCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Adults</span>
                                <strong>{formatNumber(residents.adultCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Seniors</span>
                                <strong>{formatNumber(residents.seniorCount)}</strong>
                            </div>
                        </article>

                        <article className="city-population-panel">
                            <div className="city-population-panel__title">Employment</div>
                            <div className="city-population-kv">
                                <span>Employed</span>
                                <strong>{formatNumber(residents.employedCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Students</span>
                                <strong>{formatNumber(residents.studentCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Unemployed</span>
                                <strong>{formatNumber(residents.unemployedCount)}</strong>
                            </div>
                            <div className="city-population-kv">
                                <span>Retired</span>
                                <strong>{formatNumber(residents.retiredCount)}</strong>
                            </div>
                        </article>
                    </section>

                    <section className="city-population-facts">
                        {summary.environment ? (
                            <>
                                <span className="city-population-fact-chip">
                                    Climate zone: {humanize(summary.environment.climateZone)}
                                </span>
                                <span className="city-population-fact-chip">
                                    Hemisphere: {humanize(summary.environment.hemisphere)}
                                </span>
                            </>
                        ) : null}

                        {summary.weather ? (
                            <>
                                <span className="city-population-fact-chip">
                                    Weather exposure: {humanize(summary.weather.currentType)}
                                </span>
                                <span className="city-population-fact-chip">
                                    Severity: {humanize(summary.weather.currentSeverity)}
                                </span>
                                {summary.weather.isRecoveryActive ? (
                                    <span className="city-population-fact-chip">
                                        Recovery active
                                    </span>
                                ) : null}
                            </>
                        ) : null}
                    </section>

                    <section className="city-population-timeline">
                        <article className="city-population-timeline__item">
                            <div className="city-population-timeline__label">Simulation updated</div>
                            <div className="city-population-timeline__value">
                                {formatDateTime(summary.simulation?.updatedAtUtc)}
                            </div>
                        </article>

                        <article className="city-population-timeline__item">
                            <div className="city-population-timeline__label">Environment updated</div>
                            <div className="city-population-timeline__value">
                                {formatDateTime(summary.environment?.updatedAtUtc)}
                            </div>
                        </article>

                        <article className="city-population-timeline__item">
                            <div className="city-population-timeline__label">Weather observed</div>
                            <div className="city-population-timeline__value">
                                {formatDateTime(summary.weather?.lastWeatherOccurredOnUtc)}
                            </div>
                        </article>

                        <article className="city-population-timeline__item">
                            <div className="city-population-timeline__label">Impact applied</div>
                            <div className="city-population-timeline__value">
                                {formatDateTime(summary.weather?.lastWeatherImpactAppliedAtSimTimeUtc)}
                            </div>
                        </article>
                    </section>
                </div>
            ) : null}
        </Card>
    );
}
