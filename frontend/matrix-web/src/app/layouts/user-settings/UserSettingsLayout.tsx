import {Outlet, useLocation, useNavigate} from "react-router-dom";
import {useEffect, useMemo, useRef} from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import {userSettingsNavItems} from "@shared/navigation/Items/UserSettingsItems";
import {extractMainLayoutReturnPath} from "@shared/navigation/utils/layoutExit";
import "./user-settings-layout.css";

const routeMetadata = [
    {
        match: "/userSettings/profile",
        title: "Profile",
        subtitle: "Review the identity card and public-facing details of this operator.",
    },
    {
        match: "/userSettings/security",
        title: "Security",
        subtitle: "Protect the account with email verification and password controls.",
    },
    {
        match: "/userSettings/sessions",
        title: "Sessions",
        subtitle: "Track active devices, revoke stale sessions and audit operator access.",
    },
    {
        match: "/userSettings/preferences",
        title: "Workspace setup",
        subtitle: "Shape language, theme presets and console defaults for this device.",
    },
    {
        match: "/userSettings/danger",
        title: "Danger zone",
        subtitle: "Handle destructive account actions with deliberate, high-friction controls.",
    },
];

export default function UserSettingsLayout() {
    const nav = useNavigate();
    const location = useLocation();
    const {pathname} = location;

    const fromRef = useRef<string>("/");

    useEffect(() => {
        const from = extractMainLayoutReturnPath(location.state);

        if (from) {
            fromRef.current = from;
        }
    }, [location.state]);

    const currentRouteMetadata = useMemo(() => {
        return routeMetadata.find(({match}) => pathname.startsWith(match));
    }, [pathname]);

    const topbarTitle = currentRouteMetadata?.title ?? "User settings";
    const topbarSubtitle =
        currentRouteMetadata?.subtitle ??
        "Review the profile, security posture, sessions and device defaults for this account.";

    return (
        <ShellLayout
            title="User settings"
            items={userSettingsNavItems}
            storageKey="user-settings.sidebar.collapsed"
            onBack={() => nav(fromRef.current, {replace: true})}
            topbarTitle={topbarTitle}
            topbarSubtitle={topbarSubtitle}
        >
            <Outlet/>
        </ShellLayout>
    );
}
