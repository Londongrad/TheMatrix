import { useLocation } from "react-router-dom";
import "./AdminTopbar.css";

export default function AdminTopbar() {
  const { pathname } = useLocation();
  const title = pathname.includes("/admin/users")
    ? "Users"
    : pathname.includes("/admin/roles")
    ? "Roles"
    : pathname.includes("/admin/permissions")
    ? "Permissions"
    : "Admin";

  return (
    <header className="mx-admin-topbar">
      <div>
        <div className="mx-admin-topbar__title">{title}</div>
        <div className="mx-admin-topbar__meta">
          RBAC • Identity •{" "}
          <span className="mx-admin-topbar__mono">{pathname}</span>
        </div>
      </div>
    </header>
  );
}
