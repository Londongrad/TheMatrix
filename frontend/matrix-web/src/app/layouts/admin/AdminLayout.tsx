// src/app/layouts/admin/AdminLayout.tsx
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useRef } from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import { adminNavItems } from "@shared/navigation/Items/AdminItems";
import "./admin-layout.css";

export default function AdminLayout() {
  const nav = useNavigate();
  const location = useLocation();
  const { pathname } = location;

  const topbarTitle = pathname.includes("/admin/users")
    ? "Users"
    : pathname.includes("/admin/roles")
    ? "Roles"
    : pathname.includes("/admin/permissions")
    ? "Permissions"
    : "Admin";
  const topbarSubtitle = `RBAC • Identity • ${pathname}`;

  const fromRef = useRef<string>("/");

  useEffect(() => {
    const from = (location.state as any)?.from as string | undefined;
    if (from) fromRef.current = from;
  }, [location.state]);

  return (
    <ShellLayout
      title="Matrix Admin"
      items={adminNavItems}
      storageKey="admin.sidebar.collapsed"
      onBack={() => nav(fromRef.current || "/", { replace: true })}
      topbarTitle={topbarTitle}
      topbarSubtitle={topbarSubtitle}
    >
      <Outlet />
    </ShellLayout>
  );
}
