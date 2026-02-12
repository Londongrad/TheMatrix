import {Fragment} from "react";
import {Navigate, Route} from "react-router-dom";
import CitiesPage from "@services/citycore/scenarios/classic-city/pages/CitiesPage";
import CityDetailsPage from "@services/citycore/scenarios/classic-city/pages/CityDetailsPage";
import ScenarioCatalogPage from "@services/citycore/scenarios/pages/ScenarioCatalogPage";
import {
    CITYCORE_NEW_SIMULATION_PATH,
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_DETAILS_PATH_PATTERN,
    CLASSIC_CITY_LIST_PATH,
} from "@services/citycore/scenarios/registry";

export const cityCoreRoutes = (
    <Fragment>
        <Route path={CITYCORE_SCENARIO_CATALOG_PATH} element={<ScenarioCatalogPage/>}/>
        <Route
            path={CITYCORE_NEW_SIMULATION_PATH}
            element={<Navigate to={CITYCORE_SCENARIO_CATALOG_PATH} replace/>}
        />
        <Route path={CLASSIC_CITY_LIST_PATH} element={<CitiesPage/>}/>
        <Route path={CLASSIC_CITY_DETAILS_PATH_PATTERN} element={<CityDetailsPage/>}/>
    </Fragment>
);
