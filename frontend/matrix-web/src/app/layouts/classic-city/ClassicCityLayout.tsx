import {Outlet, useLocation, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {useMemo} from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import {filterNavItems} from "@shared/permissions/filterNavItems";
import {usePermissions} from "@shared/permissions/usePermissions";
import type {NavItem} from "@shared/navigation/Sidebar/types";
import {
    SIMULATIONCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_LIST_PATH,
    getClassicCityCivilRegistryPath,
    getClassicCityDetailsPath,
    getClassicCityEducationPath,
    getClassicCityEmploymentPath,
    getClassicCityProvisioningPath,
    getClassicCityResidentsPath,
    getClassicCityResidentDossierPath,
    getClassicCitySetupPath,
    type ClassicCityResidentSection,
    type ClassicCityWorkspaceSection,
} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

const CITY_WORKSPACE_META: Record<ClassicCityWorkspaceSection, { label: string; subtitle: string }> = {
    overview: {
        label: "Overview",
        subtitle: "Lifecycle, naming, archival state, and core management controls for the current city.",
    },
    dashboard: {
        label: "Dashboard",
        subtitle: "City-wide metrics, recent activity, and simulation signals without leaving the active host.",
    },
    economy: {
        label: "Economy",
        subtitle: "Budget posture, business operators, household accounts, and cursor-based financial feeds for the city.",
    },
    map: {
        label: "Map",
        subtitle: "Canonical topology, road graph, anchors, and active world trips for the current city.",
    },
    infrastructure: {
        label: "Infrastructure",
        subtitle: "District utility slices, incident pressure, and service stability across the current city.",
    },
    population: {
        label: "Population",
        subtitle: "Resident counts, housing distribution, and wellbeing summary at the city level.",
    },
    weather: {
        label: "Weather",
        subtitle: "Atmospheric state, climate context, and weather timing for the active city.",
    },
    simulation: {
        label: "Simulation",
        subtitle: "Clock state, runtime controls, and transport diagnostics for the current simulation.",
    },
};

const RESIDENT_META: Record<ClassicCityResidentSection, { label: string; subtitle: string }> = {
    overview: {
        label: "Overview",
        subtitle: "Current resident snapshot, identity, household context, and live city-scoped state.",
    },
    relationships: {
        label: "Relationships",
        subtitle: "Spouse, parents, children, and current household links for this resident.",
    },
    career: {
        label: "Career",
        subtitle: "Employment state, workplace context, and future career history surface.",
    },
    education: {
        label: "Education",
        subtitle: "Current education state, institution context, and future study timeline surface.",
    },
    health: {
        label: "Health",
        subtitle: "Live illness, wellbeing, stress, and recovery-related resident signals.",
    },
};

function isWorkspaceSection(value: string | null): value is ClassicCityWorkspaceSection {
    return value === "overview" ||
        value === "dashboard" ||
        value === "economy" ||
        value === "map" ||
        value === "infrastructure" ||
        value === "population" ||
        value === "weather" ||
        value === "simulation";
}

function isResidentSection(value: string | null): value is ClassicCityResidentSection {
    return value === "overview" ||
        value === "relationships" ||
        value === "career" ||
        value === "education" ||
        value === "health";
}

export default function ClassicCityLayout() {
    const navigate = useNavigate();
    const location = useLocation();
    const params = useParams<{ cityId?: string; residentId?: string }>();
    const [searchParams] = useSearchParams();
    const {canAny, canAll} = usePermissions();

    const cityId = params.cityId ?? "";
    const residentId = params.residentId ?? "";
    const rawTab = searchParams.get("tab");
    const activeCitySection: ClassicCityWorkspaceSection = isWorkspaceSection(rawTab)
        ? rawTab
        : "overview";
    const activeResidentSection: ClassicCityResidentSection = isResidentSection(rawTab)
        ? rawTab
        : "overview";
    const isResidentWorkspace = cityId.length > 0 && residentId.length > 0;
    const isProvisioningWorkspace = cityId.length > 0 && location.pathname === getClassicCityProvisioningPath(cityId);
    const isCityWorkspace = cityId.length > 0 && !isResidentWorkspace && !isProvisioningWorkspace;
    const isSetupWorkspace = location.pathname.startsWith("/scenarios/classic-city/setup");

    const scenarioItems: NavItem[] = useMemo(() => {
        const items: NavItem[] = [
            {
                to: CLASSIC_CITY_LIST_PATH,
                label: "Cities",
                end: true,
                requiredPermissions: [PermissionKeys.SimulationCoreClassicCityRead],
                permissionDisplay: "disable",
            },
            {
                to: getClassicCitySetupPath(),
                label: "Compose city",
                requiredPermissions: [PermissionKeys.SimulationCoreClassicCityCreate],
                permissionDisplay: "disable",
            },
        ];

        if (isProvisioningWorkspace && cityId) {
            items.push({
                to: getClassicCityProvisioningPath(cityId),
                label: "Provisioning",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            });
        }

        return items;
    }, [cityId, isProvisioningWorkspace]);

    const cityItems: NavItem[] = useMemo(() => {
        if (!cityId) {
            return [];
        }

        return [
            {
                to: getClassicCityDetailsPath(cityId, "overview"),
                label: "Overview",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "dashboard"),
                label: "Dashboard",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                    PermissionKeys.PopulationPeopleRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "economy"),
                label: "Economy",
                end: true,
                requiredPermissions: [
                    PermissionKeys.EconomyBudgetRead,
                    PermissionKeys.EconomyBusinessesRead,
                    PermissionKeys.EconomyHouseholdAccountsRead,
                ],
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "map"),
                label: "Map",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "infrastructure"),
                label: "Infrastructure",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "population"),
                label: "Population",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.PopulationPeopleRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "weather"),
                label: "Weather",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityDetailsPath(cityId, "simulation"),
                label: "Simulation",
                end: true,
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.SimulationCoreSimulationRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityResidentsPath(cityId),
                label: "Residents",
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.PopulationPeopleRead,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityCivilRegistryPath(cityId),
                label: "Civil registry",
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.PopulationPeopleRead,
                    PermissionKeys.PopulationCivilRegistryManage,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityEmploymentPath(cityId),
                label: "Employment",
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.PopulationPeopleRead,
                    PermissionKeys.PopulationEmploymentManage,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
            {
                to: getClassicCityEducationPath(cityId),
                label: "Education",
                requiredPermissions: [
                    PermissionKeys.SimulationCoreClassicCityRead,
                    PermissionKeys.PopulationPeopleRead,
                    PermissionKeys.PopulationEducationManage,
                ],
                requiredPermissionsMode: "all",
                permissionDisplay: "disable",
            },
        ];
    }, [cityId]);

    const residentItems: NavItem[] = useMemo(() => {
        if (!cityId || !residentId) {
            return [];
        }

        return [
            {
                to: getClassicCityResidentDossierPath(cityId, residentId, "overview"),
                label: "Overview",
                end: true,
            },
            {
                to: getClassicCityResidentDossierPath(cityId, residentId, "relationships"),
                label: "Relationships",
                end: true,
            },
            {
                to: getClassicCityResidentDossierPath(cityId, residentId, "career"),
                label: "Career",
                end: true,
            },
            {
                to: getClassicCityResidentDossierPath(cityId, residentId, "education"),
                label: "Education",
                end: true,
            },
            {
                to: getClassicCityResidentDossierPath(cityId, residentId, "health"),
                label: "Health",
                end: true,
            },
        ];
    }, [cityId, residentId]);

    const items = useMemo(() => {
        const source = isResidentWorkspace
            ? residentItems
            : isCityWorkspace
                ? cityItems
                : scenarioItems;

        return filterNavItems(source, {canAny, canAll});
    }, [canAll, canAny, cityItems, isCityWorkspace, isResidentWorkspace, residentItems, scenarioItems]);

    const shellTitle = isResidentWorkspace
        ? "Resident dossier"
        : isCityWorkspace
            ? "City workspace"
            : "Classic City";

    const topbarTitle = isResidentWorkspace
        ? RESIDENT_META[activeResidentSection].label
        : isCityWorkspace
            ? CITY_WORKSPACE_META[activeCitySection].label
            : isProvisioningWorkspace
                ? "Provisioning"
                : isSetupWorkspace
                    ? "Compose city"
                    : "Cities";

    const topbarSubtitle = isResidentWorkspace
        ? RESIDENT_META[activeResidentSection].subtitle
        : isCityWorkspace
            ? CITY_WORKSPACE_META[activeCitySection].subtitle
            : isProvisioningWorkspace
                ? "Bootstrap state, handoff progress, and failed launch recovery for the selected city."
                : isSetupWorkspace
                    ? "Configure and launch a Classic City without mixing scenario setup into the global sidebar."
                    : "Scenario-level registry, launch entry point, and city host access for Classic City.";

    const handleBack = () => {
        if (isResidentWorkspace && cityId) {
            navigate(getClassicCityResidentsPath(cityId), {replace: true});
            return;
        }

        if (isCityWorkspace || isProvisioningWorkspace) {
            navigate(CLASSIC_CITY_LIST_PATH, {replace: true});
            return;
        }

        navigate(SIMULATIONCORE_SCENARIO_CATALOG_PATH, {replace: true});
    };

    return (
        <ShellLayout
            title={shellTitle}
            items={items}
            storageKey="classic-city.sidebar.collapsed"
            onBack={handleBack}
            topbarTitle={topbarTitle}
            topbarSubtitle={topbarSubtitle}
        >
            <Outlet/>
        </ShellLayout>
    );
}
