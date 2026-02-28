import type {NavItem} from "@shared/navigation/Sidebar/types";

export const userSettingsNavItems: NavItem[] = [
    {to: "/userSettings/account", label: "Account"},
    {to: "/userSettings/personalization", label: "Personalization"},
    {to: "/userSettings/security", label: "Security"},
    {to: "/userSettings/sessions", label: "Sessions"},
    {to: "/userSettings/workspace", label: "Workspace"},
    {to: "/userSettings/danger", label: "Danger zone"},
];
