import type { NavItem } from "@shared/navigation/Sidebar/types";

export const userSettingsNavItems: NavItem[] = [
  { to: "/userSettings/profile", label: "Profile" },
  { to: "/userSettings/security", label: "Security" },
  { to: "/userSettings/sessions", label: "Sessions" },
  { to: "/userSettings/preferences", label: "Preferences" },
  { to: "/userSettings/danger", label: "Danger zone" },
];
