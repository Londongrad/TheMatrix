import {useEffect, useMemo} from "react";
import {Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {CityDashboardCard} from "@services/simulationcore/scenarios/classic-city/components/CityDashboardCard";
import {CityInfrastructureCard} from "@services/simulationcore/scenarios/classic-city/components/CityInfrastructureCard";
import {CityOverviewCard} from "@services/simulationcore/scenarios/classic-city/components/CityOverviewCard";
import {
    CityPopulationSummaryCard
} from "@services/simulationcore/scenarios/classic-city/components/CityPopulationSummaryCard";
import {CityWeatherCard} from "@services/simulationcore/scenarios/classic-city/components/CityWeatherCard";
import {CityWorldMapCard} from "@services/simulationcore/scenarios/classic-city/components/CityWorldMapCard";
import {useCityDetails} from "@services/simulationcore/scenarios/classic-city/hooks/useCityDetails";
import {useCityLifecycleMutations} from "@services/simulationcore/scenarios/classic-city/hooks/useCityLifecycleMutations";
import {getCityStatusTone, isArchivedCity,} from "@services/simulationcore/scenarios/classic-city/utils/presentation";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityProvisioningPath,
} from "@services/simulationcore/scenarios/registry";
import SimulationPanel from "@services/simulationcore/simulation/components/SimulationPanel";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/simulationcore/scenarios/classic-city/styles/cities.css";
import "@services/simulationcore/scenarios/classic-city/styles/city-details.css";

const CITY_DETAILS_TABS = [
    {
        id: "dashboard",
        label: "Dashboard",
        subtitle: "City-scale metrics, resident activity, and recent simulation signals.",
    },
    {
        id: "map",
        label: "Map",
        subtitle: "Canonical topology, anchors, road graph, and active world travel over the city surface.",
    },
    {
        id: "infrastructure",
        label: "Infrastructure",
        subtitle: "District utility conditions, breakdown risk, and incident pressure across the city surface.",
    },
    {
        id: "overview",
        label: "Overview",
        subtitle: "Lifecycle, naming, archival state, and management actions.",
    },
    {
        id: "population",
        label: "Population",
        subtitle: "Residents, housing distribution, and wellbeing summary.",
    },
    {
        id: "weather",
        label: "Weather",
        subtitle: "Current atmospheric state, climate context, and weather timing.",
    },
    {
        id: "simulation",
        label: "Simulation",
        subtitle: "Clock state, runtime transport, and simulation control.",
    },
] as const;

type CityDetailsTabId = (typeof CITY_DETAILS_TABS)[number]["id"];

function isCityDetailsTab(value: string | null): value is CityDetailsTabId {
    return CITY_DETAILS_TABS.some((tab) => tab.id === value);
}

const CityDetailsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();
    const {can} = usePermissions();

    const cityQuery = useCityDetails(cityId);
    const cityMutations = useCityLifecycleMutations();
    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const canRenameCity = can(PermissionKeys.SimulationCoreClassicCityUpdate);
    const canArchiveCity = can(PermissionKeys.SimulationCoreClassicCityArchive);
    const canDeleteCity = can(PermissionKeys.SimulationCoreClassicCityDelete);
    const canControlSimulation = can(PermissionKeys.SimulationCoreSimulationControl);
    const rawTab = searchParams.get("tab");
    const focusResidentId = searchParams.get("focusResidentId") ?? "";
    const focusResidentName = searchParams.get("focusResidentName") ?? "";
    const focusTripId = searchParams.get("focusTripId") ?? "";
    const focusTripSubject = searchParams.get("focusTripSubject") ?? "";
    const focusDistrictId = searchParams.get("focusDistrictId") ?? "";
    const focusDistrictName = searchParams.get("focusDistrictName") ?? "";
    const focusAnchorIds = useMemo(() => {
        const rawValue = searchParams.get("focusAnchorIds");
        if (!rawValue) {
            return [];
        }

        return rawValue
            .split(",")
            .map((value) => value.trim())
            .filter((value) => value.length > 0);
    }, [searchParams]);
    const activeTab: CityDetailsTabId = isCityDetailsTab(rawTab)
        ? rawTab
        : "overview";
    const activeTabLabel = CITY_DETAILS_TABS.find((tab) => tab.id === activeTab)?.label ?? "Workspace";

    useEffect(() => {
        if (rawTab === activeTab) {
            return;
        }

        const next = new URLSearchParams(searchParams);
        next.set("tab", activeTab);
        setSearchParams(next, {replace: true});
    }, [activeTab, rawTab, searchParams, setSearchParams]);

    useEffect(() => {
        const scrollContainer = document.querySelector<HTMLElement>(".mx-shell__content");
        scrollContainer?.scrollTo({top: 0, behavior: "auto"});
    }, [activeTab]);

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

    function handleMapDistrictFocus(districtId: string, districtName: string) {
        const next = new URLSearchParams(searchParams);
        next.set("tab", "map");
        next.set("focusDistrictId", districtId);
        next.set("focusDistrictName", districtName);
        next.delete("focusTripId");
        next.delete("focusTripSubject");
        next.delete("focusResidentId");
        next.delete("focusResidentName");
        next.delete("focusAnchorIds");
        setSearchParams(next, {replace: false});
    }

    function handleMapTripFocus(
        tripId: string,
        tripSubject: string,
        districtId: string,
        districtName?: string,
    ) {
        const next = new URLSearchParams(searchParams);
        next.set("tab", "map");
        next.set("focusTripId", tripId);
        next.set("focusTripSubject", tripSubject);
        next.set("focusDistrictId", districtId);
        if (districtName) {
            next.set("focusDistrictName", districtName);
        } else {
            next.delete("focusDistrictName");
        }
        next.delete("focusResidentId");
        next.delete("focusResidentName");
        next.delete("focusAnchorIds");
        setSearchParams(next, {replace: false});
    }

    const renderActiveTab = () => {
        switch (activeTab) {
            case "overview":
                return (
                    <CityOverviewCard
                        city={cityQuery.data ?? null}
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
                );

            case "dashboard":
                return cityQuery.data ? (
                    <CityDashboardCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                        isArchived={isArchived}
                    />
                ) : null;

            case "map":
                return cityQuery.data ? (
                    <CityWorldMapCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                        isArchived={isArchived}
                        focusTripId={focusTripId || undefined}
                        focusTripSubject={focusTripSubject || undefined}
                        focusTravellerId={focusResidentId || undefined}
                        focusTravellerName={focusResidentName || undefined}
                        focusDistrictId={focusDistrictId || undefined}
                        focusDistrictName={focusDistrictName || undefined}
                        focusAnchorIds={focusAnchorIds}
                        onFocusDistrict={handleMapDistrictFocus}
                        onFocusTrip={handleMapTripFocus}
                    />
                ) : null;

            case "infrastructure":
                return cityQuery.data ? (
                    <CityInfrastructureCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                        isArchived={isArchived}
                    />
                ) : null;

            case "population":
                return cityQuery.data ? (
                    <CityPopulationSummaryCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                    />
                ) : null;

            case "weather":
                return cityQuery.data ? (
                    <CityWeatherCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                        isArchived={isArchived}
                    />
                ) : null;

            case "simulation":
                return cityQuery.data ? (
                    <SimulationPanel
                        simulationId={cityQuery.data.simulationId}
                        isReadOnly={isArchived}
                        canControl={canControlSimulation}
                    />
                ) : null;
        }
    };

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

    return (
        <div className="cities-page city-details-page">
            {cityQuery.error ? (
                <div className="simulationcore-error-banner" role="alert">
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

            <div
                role="region"
                aria-label={`${activeTabLabel} panel`}
                className="city-details-page__tab-panel"
            >
                {renderActiveTab()}
            </div>
        </div>
    );
};

export default CityDetailsPage;
