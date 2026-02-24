import {useMemo, useState} from "react";
import {Link, useNavigate} from "react-router-dom";
import CityList from "@services/citycore/scenarios/classic-city/components/CityList";
import {CitiesToolbar} from "@services/citycore/scenarios/classic-city/components/CitiesToolbar";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import {useCitiesQuery} from "@services/citycore/scenarios/classic-city/hooks/useCitiesQuery";
import {useProvisioningCitiesQuery} from "@services/citycore/scenarios/classic-city/hooks/useProvisioningCitiesQuery";
import {getCityStatusTone} from "@services/citycore/scenarios/classic-city/utils/presentation";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCitySetupPath,
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";

function normalize(value: string): string {
    return value.trim().toLowerCase();
}

function getCityRank(city: CityListItemView): number {
    switch (getCityStatusTone(city.status)) {
        case "failed":
            return 0;
        case "provisioning":
            return 1;
        case "active":
            return 2;
        case "unknown":
            return 3;
        case "archived":
        default:
            return 4;
    }
}

export default function CitiesPage() {
    const navigate = useNavigate();
    const {can} = usePermissions();

    const [search, setSearch] = useState("");
    const [includeArchived, setIncludeArchived] = useState(false);
    const canCreateCity = can(PermissionKeys.CityCoreClassicCityCreate);

    const citiesQuery = useCitiesQuery(includeArchived);
    const provisioningQuery = useProvisioningCitiesQuery();

    const filteredCities = useMemo(() => {
        const query = normalize(search);

        if (!query) {
            return citiesQuery.data;
        }

        return citiesQuery.data.filter((city) => {
            const name = city.name.toLowerCase();
            const cityId = city.cityId.toLowerCase();
            const simulationKind = city.simulationKind.toLowerCase();
            const status = city.status.toLowerCase();

            return name.includes(query) ||
                cityId.includes(query) ||
                simulationKind.includes(query) ||
                status.includes(query);
        });
    }, [citiesQuery.data, search]);

    const orderedCities = useMemo(() => {
        return [...filteredCities].sort((left, right) => {
            const rankDelta = getCityRank(left) - getCityRank(right);
            if (rankDelta !== 0) {
                return rankDelta;
            }

            return left.name.localeCompare(right.name, undefined, {sensitivity: "base"});
        });
    }, [filteredCities]);

    const stats = useMemo(() => {
        const readyCount = citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "active").length;
        const archivedCount = citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "archived").length;
        const provisioningCount = provisioningQuery.data.length;

        return {
            visible: orderedCities.length,
            ready: readyCount,
            provisioning: provisioningCount,
            archived: archivedCount,
        };
    }, [citiesQuery.data, orderedCities.length, provisioningQuery.data]);

    function handleOpen(city: CityListItemView) {
        const tone = getCityStatusTone(city.status);
        navigate(
            tone === "provisioning" || tone === "failed"
                ? getClassicCityProvisioningPath(city.cityId)
                : getClassicCityDetailsPath(city.cityId),
        );
    }

    return (
        <section className="cities-page">
            <header className="cities-page__header">
                <div>
                    <div className="cities-page__eyebrow">CityCore</div>
                    <h1 className="cities-page__title">Cities</h1>
                    <p className="cities-page__subtitle">
                        Operate the city registry, keep provisioning visible as a first-class handoff state, and launch
                        new worlds through the setup wizard instead of an inline sidebar form.
                    </p>
                </div>

                <div className="cities-page__header-actions">
                    {canCreateCity ? (
                        <Link className="cities-page__header-link cities-page__header-link--primary" to={getClassicCitySetupPath()}>
                            Compose Classic City
                        </Link>
                    ) : null}

                    <Link className="cities-page__header-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                        Scenario catalog
                    </Link>
                </div>
            </header>

            <div className="cities-metrics" aria-label="City registry summary">
                <article className="cities-metric-card">
                    <span className="cities-metric-card__label">Visible now</span>
                    <strong className="cities-metric-card__value">{stats.visible}</strong>
                    <span className="cities-metric-card__hint">Matches the current search query and archive scope.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--active">
                    <span className="cities-metric-card__label">Ready</span>
                    <strong className="cities-metric-card__value">{stats.ready}</strong>
                    <span className="cities-metric-card__hint">Cities already handed off to live monitoring.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--provisioning">
                    <span className="cities-metric-card__label">Provisioning</span>
                    <strong className="cities-metric-card__value">{stats.provisioning}</strong>
                    <span className="cities-metric-card__hint">
                        Includes in-flight launches and failed handoffs that still need attention.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--archived">
                    <span className="cities-metric-card__label">Archived</span>
                    <strong className="cities-metric-card__value">{stats.archived}</strong>
                    <span className="cities-metric-card__hint">Inactive records retained for review or cleanup.</span>
                </article>
            </div>

            <div className="cities-card cities-card--registry">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Provisioning handoff queue</h2>
                        <p className="cities-card__subtitle">
                            New launches and failed bootstrap attempts stay here until they are either handed off to
                            monitoring or explicitly resolved.
                        </p>
                    </div>

                    <Button
                        type="button"
                        variant="default"
                        onClick={() => {
                            void provisioningQuery.refetch();
                        }}
                        disabled={provisioningQuery.isLoading}
                    >
                        {provisioningQuery.isLoading ? "Refreshing..." : "Refresh queue"}
                    </Button>
                </div>

                {provisioningQuery.error ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Failed to load provisioning queue</div>
                            <div>{provisioningQuery.error}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                void provisioningQuery.refetch();
                            }}
                        >
                            Retry
                        </Button>
                    </div>
                ) : null}

                {!provisioningQuery.error && provisioningQuery.isLoading && provisioningQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Loading provisioning queue</div>
                        <div className="cities-empty-state__text">
                            Fetching in-flight launches and failed handoffs that still need operator attention.
                        </div>
                    </div>
                ) : null}

                {!provisioningQuery.error && !provisioningQuery.isLoading && provisioningQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Provisioning queue is clear</div>
                        <div className="cities-empty-state__text">
                            There are no in-flight or failed launches right now. New scenario launches will appear here
                            until they are ready for live monitoring.
                        </div>
                    </div>
                ) : null}

                {!provisioningQuery.error && provisioningQuery.data.length > 0 ? (
                    <CityList cities={provisioningQuery.data} onOpen={handleOpen}/>
                ) : null}
            </div>

            <div className="cities-card cities-card--registry">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">City registry</h2>
                        <p className="cities-card__subtitle">
                            Only ready cities and archived records live here. Provisioning hosts are surfaced in the
                            dedicated handoff queue instead of mixing unfinished launches into the main registry.
                        </p>
                    </div>
                </div>

                <CitiesToolbar
                    search={search}
                    includeArchived={includeArchived}
                    isRefreshing={citiesQuery.isLoading}
                    onSearchChange={setSearch}
                    onIncludeArchivedChange={setIncludeArchived}
                    onRefresh={() => {
                        void citiesQuery.refetch();
                    }}
                />

                {citiesQuery.error ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Failed to load cities</div>
                            <div>{citiesQuery.error}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                void citiesQuery.refetch();
                            }}
                        >
                            Retry
                        </Button>
                    </div>
                ) : null}

                {!citiesQuery.error && citiesQuery.isLoading && citiesQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Loading city registry</div>
                        <div className="cities-empty-state__text">
                            Fetching current city records and lifecycle handoff states.
                        </div>
                    </div>
                ) : null}

                {!citiesQuery.error && !citiesQuery.isLoading ? (
                    <CityList cities={orderedCities} onOpen={handleOpen}/>
                ) : null}
            </div>
        </section>
    );
}
