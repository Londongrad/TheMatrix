import type { NavItem } from "@shared/navigation/Sidebar/types";

export const mainNavItems: NavItem[] = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/citizens", label: "Citizens" },

  // важное: сохраняем "откуда пришёл" при входе в /admin
  { to: "/admin", label: "Admin panel", getState: (path) => ({ from: path }) },
];
