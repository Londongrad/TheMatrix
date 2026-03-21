import type {NavItem} from "@shared/navigation/Sidebar/types";
import {SIMULATIONCORE_SCENARIO_CATALOG_PATH} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

export const mainNavItems: NavItem[] = [
    {to: "/", label: "Dashboard", end: true},
    {
        to: SIMULATIONCORE_SCENARIO_CATALOG_PATH,
        label: "Scenarios",
        requiredPermissions: [PermissionKeys.SimulationCoreScenariosCatalogRead],
        permissionDisplay: "disable",
    },
    // важное: сохраняем "откуда пришёл" при входе в /admin
    {
        to: "/admin",
        label: "Admin panel",
        getState: (path) => ({from: path}),
        requiredPermissions: [
            PermissionKeys.IdentityUsersRead,
            PermissionKeys.IdentityRolesList,
            PermissionKeys.IdentityPermissionsCatalogRead,
        ],
        requiredPermissionsMode: "any",
        permissionDisplay: "hide",
    },
];
