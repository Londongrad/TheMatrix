import {useMemo, useState} from "react";
import {useNavigate} from "react-router-dom";
import CityList from "@services/citycore/cities/components/CityList";
import {CitiesToolbar} from "@services/citycore/cities/components/CitiesToolbar";
import {CreateCityForm} from "@services/citycore/cities/components/CreateCityForm";
import {useCitiesQuery} from "@services/citycore/cities/hooks/useCitiesQuery";
import {useCityMutations} from "@services/citycore/cities/hooks/useCityMutations";
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

            return (
                name.includes(query) ||
                cityId.includes(query) ||
                status.includes(query)
            );
        });
    }, [citiesQuery.data, search]);

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
                        Manage cities and open each city simulation.
                    </p>
                </div>
            </header>

            <div className="cities-page__layout">
                <div className="cities-page__main">
                    <div className="cities-card">
                        <div className="cities-card__header">
                            <h2 className="cities-card__title">City registry</h2>
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

                        {citiesQuery.error && (
                            <div className="cities-error-banner" role="alert">
                                <div className="cities-error-banner__content">
                                    <div className="cities-error-banner__title">
                                        Failed to load cities
                                    </div>
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
                        )}

                        {!citiesQuery.error && citiesQuery.isLoading && citiesQuery.data.length === 0 && (
                            <div className="cities-empty-state">Loading cities...</div>
                        )}

                        {!citiesQuery.error && !citiesQuery.isLoading && (
                            <CityList cities={filteredCities} onOpen={handleOpen}/>
                        )}
                    </div>
                </div>

                <aside className="cities-page__sidebar">
                    <div className="cities-card">
                        <div className="cities-card__header">
                            <h2 className="cities-card__title">Create city</h2>
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
