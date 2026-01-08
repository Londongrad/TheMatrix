import type { ReactNode } from "react";
import AdminSidebar from "./components/AdminSidebar";
import AdminTopbar from "./components/AdminTopbar";
import "./admin-shell.css";

export default function AdminShell({ children }: { children: ReactNode }) {
  return (
    <div className="mx-admin-shell">
      <AdminSidebar />
      <div className="mx-admin-shell__main">
        <AdminTopbar />
        <main className="mx-admin-shell__content">{children}</main>
      </div>
    </div>
  );
}
