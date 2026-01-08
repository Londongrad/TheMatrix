import type { NavItem } from "@shared/navigation/Sidebar/types";

export const adminNavItems: NavItem[] = [
  { to: "/admin/users", label: "Users" },
  { to: "/admin/roles", label: "Roles" },
  { to: "/admin/permissions", label: "Permissions" },
];
