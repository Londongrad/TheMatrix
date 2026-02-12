import type {NavItem} from "@shared/navigation/Sidebar/types";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_LIST_PATH,
} from "@services/citycore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";

export const mainNavItems: NavItem[] = [
    {to: "/", label: "Dashboard", end: true},
    {to: CITYCORE_SCENARIO_CATALOG_PATH, label: "Scenarios"},
    {to: CLASSIC_CITY_LIST_PATH, label: "Cities"},
    {
        to: "/citizens",
        label: "Citizens",
        requiredPermissions: [PermissionKeys.PopulationPeopleRead],
        permissionDisplay: "disable",
    },

    // важное: сохраняем "откуда пришёл" при входе в /admin
    {
        to: "/admin",
        label: "Admin panel",
        getState: (path) => ({from: path}),
        requiredPermissions: [PermissionKeys.IdentityAdminAccess],
        permissionDisplay: "hide",
    },
];
