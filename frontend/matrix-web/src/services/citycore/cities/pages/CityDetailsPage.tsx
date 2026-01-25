import {useNavigate, useParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/cities/components/CityDetailsHeader";
import {CityOverviewCard} from "@services/citycore/cities/components/CityOverviewCard";
import {useCityDetails} from "@services/citycore/cities/hooks/useCityDetails";
import {useCityMutations} from "@services/citycore/cities/hooks/useCityMutations";
import SimulationPanel from "@services/citycore/simulation/components/SimulationPanel";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/cities/styles/city-details.css";

const CityDetailsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();

    const cityQuery = useCityDetails(cityId);
    const cityMutations = useCityMutations();

    async function handleRename(name: string) {
        if (!cityId) {
            return;
        }

        const isOk = await cityMutations.rename(cityId, name);

        if (isOk) {
            await cityQuery.refetch();
        }
    }

    async function handleArchive() {
        if (!cityId) {
            return;
        }

        const isOk = await cityMutations.archive(cityId);

        if (isOk) {
            await cityQuery.refetch();
        }
    }

    async function handleDelete() {
        if (!cityId) {
            return;
        }

        const isOk = await cityMutations.delete(cityId);

        if (isOk) {
            navigate("/cities");
        }
    }

    return (
        <div className="cities-page">
            <CityDetailsHeader
                title={cityQuery.data?.name ?? "City details"}
                cityId={cityQuery.data?.cityId ?? cityId}
            />

            {cityQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{cityQuery.error}</span>
                    <Button
                        type="button"
                        variant="primary"
                        onClick={() => {
                            void cityQuery.refetch();
                        }}
                    >
                        Retry
                    </Button>
                </div>
            ) : null}

            <CityOverviewCard
                city={cityQuery.data}
                isLoading={cityQuery.isLoading}
                isSubmitting={cityMutations.isSubmitting}
                mutationError={cityMutations.error}
                onClearMutationError={cityMutations.clearError}
                onRename={handleRename}
                onArchive={handleArchive}
                onDelete={handleDelete}
            />

            {cityQuery.data ? <SimulationPanel cityId={cityQuery.data.cityId}/> : null}
        </div>
    );
};

export default CityDetailsPage;
