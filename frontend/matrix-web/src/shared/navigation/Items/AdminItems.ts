import type { NavItem } from "@shared/navigation/Sidebar/types";
import { PermissionKeys } from "@shared/permissions/permissionKeys";

export const adminNavItems: NavItem[] = [
  {
    to: "/admin/users",
    label: "Users",
    requiredPermissions: [PermissionKeys.IdentityUsersRead],
    permissionDisplay: "hide",
  },
  {
    to: "/admin/roles",
    label: "Roles",
    requiredPermissions: [PermissionKeys.IdentityRolesList],
    permissionDisplay: "hide",
  },
  {
    to: "/admin/permissions",
    label: "Permissions",
    requiredPermissions: [PermissionKeys.IdentityPermissionsCatalogRead],
    permissionDisplay: "hide",
  },
];
