// src/app/layouts/user-settings/UserSettingsLayout.tsx
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useMemo, useRef } from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import { userSettingsNavItems } from "@shared/navigation/Items/UserSettingsItems";
import "./user-settings-layout.css";

const titleMap: Record<string, string> = {
  "/userSettings/profile": "Profile",
  "/userSettings/security": "Security",
  "/userSettings/sessions": "Sessions",
  "/userSettings/preferences": "Preferences",
  "/userSettings/danger": "Danger zone",
};

export default function UserSettingsLayout() {
  const nav = useNavigate();
  const location = useLocation();
  const { pathname } = location;

  const fromRef = useRef<string>("/");

  useEffect(() => {
    const from = (location.state as { from?: string } | null)?.from;
    if (from) fromRef.current = from;
  }, [location.state]);

  const topbarTitle = useMemo(() => {
    const match = Object.keys(titleMap).find((key) => pathname.includes(key));
    return match ? titleMap[match] : "User settings";
  }, [pathname]);

  const topbarSubtitle = `Account • Preferences • ${pathname}`;

  return (
    <ShellLayout
      title="User settings"
      items={userSettingsNavItems}
      storageKey="user-settings.sidebar.collapsed"
      onBack={() => nav(fromRef.current || "/", { replace: true })}
      topbarTitle={topbarTitle}
      topbarSubtitle={topbarSubtitle}
    >
      <Outlet />
    </ShellLayout>
  );
}
