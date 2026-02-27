import {Fragment} from "react";
import {Navigate, Route} from "react-router-dom";
import {RequireRoutePermission} from "@app/router/guards/RequireRoutePermission";
import CitiesPage from "@services/citycore/scenarios/classic-city/pages/CitiesPage";
import CityDetailsPage from "@services/citycore/scenarios/classic-city/pages/CityDetailsPage";
import CityResidentsPage from "@services/citycore/scenarios/classic-city/pages/CityResidentsPage";
import ClassicCityProvisioningPage from "@services/citycore/scenarios/classic-city/pages/ClassicCityProvisioningPage";
import ClassicCityProvisioningSessionPage from "@services/citycore/scenarios/classic-city/pages/ClassicCityProvisioningSessionPage";
import ClassicCitySetupPage from "@services/citycore/scenarios/classic-city/pages/ClassicCitySetupPage";
import ScenarioCatalogPage from "@services/citycore/scenarios/pages/ScenarioCatalogPage";
import {
    CITYCORE_NEW_SIMULATION_PATH,
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_DETAILS_PATH_PATTERN,
    CLASSIC_CITY_LIST_PATH,
    CLASSIC_CITY_PROVISIONING_PATH_PATTERN,
    CLASSIC_CITY_RESIDENTS_PATH_PATTERN,
    CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN,
    CLASSIC_CITY_SETUP_PATH,
    CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN,
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

export const cityCoreRoutes = (
    <Fragment>
        <Route
            path={CITYCORE_SCENARIO_CATALOG_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.CityCoreScenariosCatalogRead]}
                >
                    <ScenarioCatalogPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CITYCORE_NEW_SIMULATION_PATH}
            element={<Navigate to={CITYCORE_SCENARIO_CATALOG_PATH} replace/>}
        />
        <Route
            path={CLASSIC_CITY_SETUP_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.CityCoreClassicCityCreate]}
                >
                    <ClassicCitySetupPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_SETUP_SESSION_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.CityCoreClassicCityCreate]}
                >
                    <ClassicCitySetupPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_SETUP_PROVISIONING_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.CityCoreClassicCityCreate]}
                >
                    <ClassicCityProvisioningSessionPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_LIST_PATH}
            element={
                <RequireRoutePermission
                    permissions={[PermissionKeys.CityCoreClassicCityRead]}
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
                        PermissionKeys.CityCoreClassicCityRead,
                        PermissionKeys.CityCoreSimulationRead,
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
                        PermissionKeys.CityCoreClassicCityRead,
                        PermissionKeys.CityCoreSimulationRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityDetailsPage/>
                </RequireRoutePermission>
            }
        />
        <Route
            path={CLASSIC_CITY_RESIDENTS_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.CityCoreClassicCityRead,
                        PermissionKeys.PopulationPeopleRead,
                    ]}
                    permissionMatchMode="all"
                >
                    <CityResidentsPage/>
                </RequireRoutePermission>
            }
        />
    </Fragment>
);
