import { NavLink, useNavigate } from "react-router-dom";
import { IconKey, IconShield, IconUsers } from "./icons";
import AdminButton from "./AdminButton";
import "./AdminSidebar.css";

export default function AdminSidebar() {
  const nav = useNavigate();

  const goBack = () => {
    nav("/", { replace: true });
  };

  return (
    <aside className="mx-admin-sidebar" aria-label="Admin navigation">
      <div className="mx-admin-sidebar__brand">
        <div className="mx-admin-sidebar__mark" aria-hidden="true" />
        <div>
          <div className="mx-admin-sidebar__title">Matrix Console</div>
          <div className="mx-admin-sidebar__sub">Admin Panel</div>
        </div>
      </div>

      <nav className="mx-admin-sidebar__nav">
        <NavItem to="/admin/users" icon={<IconUsers />} label="Users" />
        <NavItem to="/admin/roles" icon={<IconShield />} label="Roles" />
        <NavItem
          to="/admin/permissions"
          icon={<IconKey />}
          label="Permissions"
        />
      </nav>

      <div className="mx-admin-sidebar__exit">
        <AdminButton onClick={goBack}>← Exit admin panel</AdminButton>
      </div>

      <div className="mx-admin-sidebar__footer">
        <div className="mx-admin-sidebar__chip">
          <span className="mx-admin-sidebar__dot" />
          Connected
        </div>
        <div className="mx-admin-sidebar__small">RBAC • Identity</div>
      </div>
    </aside>
  );
}

function NavItem({
  to,
  icon,
  label,
}: {
  to: string;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `mx-admin-sidebar__item${isActive ? " is-active" : ""}`
      }
    >
      <span className="mx-admin-sidebar__icon" aria-hidden="true">
        {icon}
      </span>
      <span className="mx-admin-sidebar__label">{label}</span>
      <span className="mx-admin-sidebar__glow" aria-hidden="true" />
    </NavLink>
  );
}
