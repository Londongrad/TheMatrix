import {useMemo, useState} from "react";
import {useNavigate} from "react-router-dom";
import CityList from "@services/citycore/cities/components/CityList";
import {CitiesToolbar} from "@services/citycore/cities/components/CitiesToolbar";
import {CreateCityForm} from "@services/citycore/cities/components/CreateCityForm";
import {useCitiesQuery} from "@services/citycore/cities/hooks/useCitiesQuery";
import {useCityMutations} from "@services/citycore/cities/hooks/useCityMutations";
import {isArchivedCity} from "@services/citycore/cities/utils/presentation";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/cities/styles/cities.css";

function normalize(value: string): string {
    return value.trim().toLowerCase();
}

export default function CitiesPage() {
    const navigate = useNavigate();

    const [search, setSearch] = useState("");
    const [includeArchived, setIncludeArchived] = useState(false);

    const citiesQuery = useCitiesQuery(includeArchived);
    const cityMutations = useCityMutations();

    const filteredCities = useMemo(() => {
        const query = normalize(search);

        if (!query) {
            return citiesQuery.data;
        }

        return citiesQuery.data.filter((city) => {
            const name = city.name.toLowerCase();
            const cityId = city.cityId.toLowerCase();
            const status = city.status.toLowerCase();

            return name.includes(query) || cityId.includes(query) || status.includes(query);
        });
    }, [citiesQuery.data, search]);

    const orderedCities = useMemo(() => {
        return [...filteredCities].sort((left, right) => {
            const leftArchived = isArchivedCity(left.status);
            const rightArchived = isArchivedCity(right.status);

            if (leftArchived !== rightArchived) {
                return leftArchived ? 1 : -1;
            }

            return left.name.localeCompare(right.name, undefined, {sensitivity: "base"});
        });
    }, [filteredCities]);

    const stats = useMemo(() => {
        const allCities = citiesQuery.data;
        const archivedCount = allCities.filter((city) => isArchivedCity(city.status)).length;
        const activeCount = allCities.length - archivedCount;

        return {
            visible: orderedCities.length,
            active: activeCount,
            archived: archivedCount,
        };
    }, [citiesQuery.data, orderedCities.length]);

    async function handleCreated(response: { cityId: string }) {
        navigate(`/cities/${response.cityId}`);
    }

    function handleOpen(cityId: string) {
        navigate(`/cities/${cityId}`);
    }

    return (
        <section className="cities-page">
            <header className="cities-page__header">
                <div>
                    <div className="cities-page__eyebrow">CityCore</div>
                    <h1 className="cities-page__title">Cities</h1>
                    <p className="cities-page__subtitle">
                        Operate the city registry, separate live simulations from archived records, and launch new
                        timelines without layout drift.
                    </p>
                </div>
            </header>

            <div className="cities-metrics" aria-label="City registry summary">
                <article className="cities-metric-card">
                    <span className="cities-metric-card__label">Visible now</span>
                    <strong className="cities-metric-card__value">{stats.visible}</strong>
                    <span className="cities-metric-card__hint">Matches current search and archive filter.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--active">
                    <span className="cities-metric-card__label">Active</span>
                    <strong className="cities-metric-card__value">{stats.active}</strong>
                    <span className="cities-metric-card__hint">Cities with editable simulation controls.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--archived">
                    <span className="cities-metric-card__label">Archived</span>
                    <strong className="cities-metric-card__value">{stats.archived}</strong>
                    <span className="cities-metric-card__hint">Inactive records retained for review or cleanup.</span>
                </article>
            </div>

            <div className="cities-page__layout">
                <div className="cities-page__main">
                    <div className="cities-card cities-card--registry">
                        <div className="cities-card__header">
                            <div>
                                <h2 className="cities-card__title">City registry</h2>
                                <p className="cities-card__subtitle">
                                    Active cities stay at the top. Archived cities remain accessible but visually
                                    separated.
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
                                    Fetching current city records and simulation availability.
                                </div>
                            </div>
                        ) : null}

                        {!citiesQuery.error && !citiesQuery.isLoading ? (
                            <CityList cities={orderedCities} onOpen={handleOpen}/>
                        ) : null}
                    </div>
                </div>

                <aside className="cities-page__sidebar">
                    <div className="cities-card cities-card--sticky">
                        <div className="cities-card__header">
                            <div>
                                <h2 className="cities-card__title">Create city</h2>
                                <p className="cities-card__subtitle">
                                    Define the city identity and choose the simulation baseline before launch.
                                </p>
                            </div>
                        </div>

                        <CreateCityForm
                            isSubmitting={cityMutations.isSubmitting}
                            submitError={cityMutations.error}
                            onSubmit={cityMutations.create}
                            onCreated={handleCreated}
                            onClearSubmitError={cityMutations.clearError}
                        />
                    </div>
                </aside>
            </div>
        </section>
    );
}
