import {useNavigate, useParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/cities/components/CityDetailsHeader";
import {CityOverviewCard} from "@services/citycore/cities/components/CityOverviewCard";
import {useCityDetails} from "@services/citycore/cities/hooks/useCityDetails";
import {useCityMutations} from "@services/citycore/cities/hooks/useCityMutations";
import {isArchivedCity} from "@services/citycore/cities/utils/presentation";
import SimulationPanel from "@services/citycore/simulation/components/SimulationPanel";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/cities/styles/cities.css";
import "@services/citycore/cities/styles/city-details.css";

const CityDetailsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();

    const cityQuery = useCityDetails(cityId);
    const cityMutations = useCityMutations();
    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);

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
        <div className="cities-page city-details-page">
            <CityDetailsHeader
                title={cityQuery.data?.name ?? "City details"}
                cityId={cityQuery.data?.cityId ?? cityId}
                simulationKind={cityQuery.data?.simulationKind}
                status={cityQuery.data?.status}
                archivedAtUtc={cityQuery.data?.archivedAtUtc}
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

            {cityQuery.data ? (
                <SimulationPanel
                    simulationId={cityQuery.data.simulationId || cityQuery.data.cityId}
                    isReadOnly={isArchived}
                    readOnlyMessage="This city is archived. Simulation time is shown as a snapshot and control mutations are disabled."
                />
            ) : null}
        </div>
    );
};

export default CityDetailsPage;
