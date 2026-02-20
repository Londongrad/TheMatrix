import {Fragment} from "react";
import {Navigate, Route} from "react-router-dom";
import {RequireRoutePermission} from "@app/router/guards/RequireRoutePermission";
import CitiesPage from "@services/citycore/scenarios/classic-city/pages/CitiesPage";
import CityDetailsPage from "@services/citycore/scenarios/classic-city/pages/CityDetailsPage";
import ScenarioCatalogPage from "@services/citycore/scenarios/pages/ScenarioCatalogPage";
import {
    CITYCORE_NEW_SIMULATION_PATH,
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_DETAILS_PATH_PATTERN,
    CLASSIC_CITY_LIST_PATH,
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
            path={CLASSIC_CITY_DETAILS_PATH_PATTERN}
            element={
                <RequireRoutePermission
                    permissions={[
                        PermissionKeys.CityCoreClassicCityRead,
                        PermissionKeys.CityCoreSimulationRead,
                    ]}
                    mode="all"
                >
                    <CityDetailsPage/>
                </RequireRoutePermission>
            }
        />
    </Fragment>
);
