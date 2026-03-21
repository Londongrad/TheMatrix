import {Fragment} from "react";
import {Navigate, Route} from "react-router-dom";
import {RequireRoutePermission} from "@app/router/guards/RequireRoutePermission";
import CitiesPage from "@services/simulationcore/scenarios/classic-city/pages/CitiesPage";
import CityCivilRegistryPage from "@services/simulationcore/scenarios/classic-city/pages/CityCivilRegistryPage";
import CityDetailsPage from "@services/simulationcore/scenarios/classic-city/pages/CityDetailsPage";
import CityEducationPage from "@services/simulationcore/scenarios/classic-city/pages/CityEducationPage";
import CityEmploymentPage from "@services/simulationcore/scenarios/classic-city/pages/CityEmploymentPage";
import CityResidentDossierPage from "@services/simulationcore/scenarios/classic-city/pages/CityResidentDossierPage";
import CityResidentsPage from "@services/simulationcore/scenarios/classic-city/pages/CityResidentsPage";
import ClassicCityProvisioningPage from "@services/simulationcore/scenarios/classic-city/pages/ClassicCityProvisioningPage";
import ClassicCityProvisioningSessionPage
    from "@services/simulationcore/scenarios/classic-city/pages/ClassicCityProvisioningSessionPage";
import ClassicCitySetupPage from "@services/simulationcore/scenarios/classic-city/pages/ClassicCitySetupPage";
import ScenarioCatalogPage from "@services/simulationcore/scenarios/pages/ScenarioCatalogPage";
import {
    SIMULATIONCORE_NEW_SIMULATION_PATH,
    SIMULATIONCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_CIVIL_REGISTRY_PATH_PATTERN,
    CLASSIC_CITY_DETAILS_PATH_PATTERN,
    CLASSIC_CITY_EDUCATION_PATH_PATTERN,
    CLASSIC_CITY_EMPLOYMENT_PATH_PATTERN,
    CLASSIC_CITY_LIST_PATH,
    CLASSIC_CITY_PROVISIONING_PATH_PATTERN,
    CLASSIC_CITY_RESIDENT_DOSSIER_PATH_PATTERN,
    CLASSIC_CITY_RESIDENTS_PATH_PATTERN,
    CLASSIC_CITY_SETUP_PATH,
    CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN,
    CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN,
} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

export const simulationCoreCatalogRoutes = (
    <Fragment>
        <Route
            path={SIMULATIONCORE_SCENARIO_CATALOG_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.SimulationCoreScenariosCatalogRead]}
                >
                    <ScenarioCatalogPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={SIMULATIONCORE_NEW_SIMULATION_PATH}
            element={<Navigate to={SIMULATIONCORE_SCENARIO_CATALOG_PATH} replace/>}
        />
    </Fragment>
);

export const classicCityRoutes = (
    <Fragment>
        <Route
            path={CLASSIC_CITY_SETUP_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.SimulationCoreClassicCityCreate]}
                >
                    <ClassicCitySetupPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.SimulationCoreClassicCityCreate]}
                >
                    <ClassicCitySetupPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.SimulationCoreClassicCityCreate]}
                >
                    <ClassicCityProvisioningSessionPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_LIST_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.SimulationCoreClassicCityRead]}
                >
                    <CitiesPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_PROVISIONING_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.SimulationCoreSimulationRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <ClassicCityProvisioningPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_DETAILS_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.SimulationCoreSimulationRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityDetailsPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_EDUCATION_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                        PermissionKeys.PopulationEducationManage,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityEducationPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_EMPLOYMENT_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                        PermissionKeys.PopulationEmploymentManage,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityEmploymentPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_CIVIL_REGISTRY_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                        PermissionKeys.PopulationCivilRegistryManage,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityCivilRegistryPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_RESIDENTS_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityResidentsPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_RESIDENT_DOSSIER_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.SimulationCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityResidentDossierPage/>
                </RequireRoutePermission>
            }
        />
    </Fragment>
);
