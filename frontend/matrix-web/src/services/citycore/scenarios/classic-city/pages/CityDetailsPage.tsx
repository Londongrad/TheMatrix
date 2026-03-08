import {useEffect, useMemo} from "react";
import {Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {CityDashboardCard} from "@services/citycore/scenarios/classic-city/components/CityDashboardCard";
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
    getClassicCityResidentsPath,
} from "@services/citycore/scenarios/registry";
import SimulationPanel from "@services/citycore/simulation/components/SimulationPanel";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";

const CITY_DETAILS_TABS = [
    {
        id: "dashboard",
        label: "Dashboard",
        subtitle: "City-scale metrics, resident activity, and recent simulation signals.",
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
    const canRenameCity = can(PermissionKeys.CityCoreClassicCityUpdate);
    const canArchiveCity = can(PermissionKeys.CityCoreClassicCityArchive);
    const canDeleteCity = can(PermissionKeys.CityCoreClassicCityDelete);
    const canControlSimulation = can(PermissionKeys.CityCoreSimulationControl);
    const canReadResidents = can(PermissionKeys.PopulationPeopleRead);
    const rawTab = searchParams.get("tab");
    const activeTab: CityDetailsTabId = isCityDetailsTab(rawTab)
        ? rawTab
        : "overview";
    const activeTabMeta = useMemo(
        () => CITY_DETAILS_TABS.find((tab) => tab.id === activeTab) ?? CITY_DETAILS_TABS[0],
        [activeTab]
    );

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

    const setActiveTab = (nextTab: CityDetailsTabId) => {
        if (nextTab === activeTab) {
            return;
        }

        const next = new URLSearchParams(searchParams);
        next.set("tab", nextTab);
        setSearchParams(next);
    };

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

            case "population":
                return cityQuery.data ? (
                    <CityPopulationSummaryCard
                        cityId={cityQuery.data.cityId}
                        cityName={cityQuery.data.name}
                    />
                ) : null;

            case "weather":
                return cityQuery.data ? (
                    <CityWeatherCard cityId={cityQuery.data.cityId}/>
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
            <CityDetailsHeader
                title={cityQuery.data?.name ?? "City details"}
                cityId={cityQuery.data?.cityId ?? cityId}
                simulationKind={cityQuery.data?.simulationKind}
                status={cityQuery.data?.status}
                archivedAtUtc={cityQuery.data?.archivedAtUtc}
                links={[
                    ...(cityQuery.data && canReadResidents
                        ? [{to: getClassicCityResidentsPath(cityQuery.data.cityId), label: "Residents"}]
                        : []),
                    {to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"},
                ]}
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

            <section className="city-details-tabs" aria-label="City monitoring workspace">
                <div className="city-details-tabs__list" role="tablist" aria-label="City workspace sections">
                    {CITY_DETAILS_TABS.map((tab) => {
                        const isActive = tab.id === activeTab;

                        return (
                            <button
                                key={tab.id}
                                type="button"
                                role="tab"
                                aria-selected={isActive}
                                aria-controls={`city-details-panel-${tab.id}`}
                                id={`city-details-tab-${tab.id}`}
                                className={`city-details-tabs__button${isActive ? " is-active" : ""}`}
                                onClick={() => {
                                    setActiveTab(tab.id);
                                }}
                            >
                                <span className="city-details-tabs__button-label">{tab.label}</span>
                                <span className="city-details-tabs__button-hint">{tab.subtitle}</span>
                            </button>
                        );
                    })}
                </div>

                <div className="city-details-tabs__subtitle">
                    <strong>{activeTabMeta.label}</strong>
                    <span>{activeTabMeta.subtitle}</span>
                </div>
            </section>

            <div
                id={`city-details-panel-${activeTab}`}
                role="tabpanel"
                aria-labelledby={`city-details-tab-${activeTab}`}
                className="city-details-page__tab-panel"
            >
                {renderActiveTab()}
            </div>
        </div>
    );
};

export default CityDetailsPage;
