// src/app/layouts/admin/AdminLayout.tsx
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useRef } from "react";
import ShellLayout from "@shared/ui/layouts/ShellLayout/ShellLayout";
import { adminNavItems } from "@shared/navigation/Items/AdminItems";
import "./admin-layout.css";

export default function AdminLayout() {
  const nav = useNavigate();
  const location = useLocation();

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
    >
      <Outlet />
    </ShellLayout>
  );
}
