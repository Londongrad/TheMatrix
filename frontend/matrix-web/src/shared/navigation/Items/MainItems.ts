import type { NavItem } from "@shared/navigation/Sidebar/types";
import { PermissionKeys } from "@shared/permissions/permissionKeys";

export const mainNavItems: NavItem[] = [
  { to: "/", label: "Dashboard", end: true },
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
    getState: (path) => ({ from: path }),
    requiredPermissions: [PermissionKeys.IdentityAdminAccess],
    permissionDisplay: "hide",
  },
];
