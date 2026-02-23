import {Navigate, useNavigate, useParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {CityOverviewCard} from "@services/citycore/scenarios/classic-city/components/CityOverviewCard";
import {CityPopulationSummaryCard} from "@services/citycore/scenarios/classic-city/components/CityPopulationSummaryCard";
import {CityWeatherCard} from "@services/citycore/scenarios/classic-city/components/CityWeatherCard";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityLifecycleMutations} from "@services/citycore/scenarios/classic-city/hooks/useCityLifecycleMutations";
import {
    getCityStatusTone,
    isArchivedCity,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityProvisioningPath,
} from "@services/citycore/scenarios/registry";
import SimulationPanel from "@services/citycore/simulation/components/SimulationPanel";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";

const CityDetailsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();
    const {can} = usePermissions();

    const cityQuery = useCityDetails(cityId);
    const cityMutations = useCityLifecycleMutations();
    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const canRenameCity = can(PermissionKeys.CityCoreClassicCityUpdate);
    const canArchiveCity = can(PermissionKeys.CityCoreClassicCityArchive);
    const canDeleteCity = can(PermissionKeys.CityCoreClassicCityDelete);
    const canControlSimulation = can(PermissionKeys.CityCoreSimulationControl);

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

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
            navigate(CLASSIC_CITY_LIST_PATH);
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
                canRename={canRenameCity}
                canArchive={canArchiveCity}
                canDelete={canDeleteCity}
                onClearMutationError={cityMutations.clearError}
                onRename={handleRename}
                onArchive={handleArchive}
                onDelete={handleDelete}
            />

            {cityQuery.data ? (
                <CityPopulationSummaryCard
                    cityId={cityQuery.data.cityId}
                    cityName={cityQuery.data.name}
                    isArchived={isArchived}
                />
            ) : null}

            {cityQuery.data ? (
                <CityWeatherCard
                    cityId={cityQuery.data.cityId}
                    cityName={cityQuery.data.name}
                    climateZone={cityQuery.data.climateZone}
                    hemisphere={cityQuery.data.hemisphere}
                    utcOffsetMinutes={cityQuery.data.utcOffsetMinutes}
                    isArchived={isArchived}
                />
            ) : null}

            {cityQuery.data ? (
                <SimulationPanel
                    simulationId={cityQuery.data.simulationId || cityQuery.data.cityId}
                    isReadOnly={isArchived}
                    canControl={canControlSimulation}
                    readOnlyMessage="This city is archived. Simulation time is shown as a snapshot and control mutations are disabled."
                />
            ) : null}
        </div>
    );
};

export default CityDetailsPage;
