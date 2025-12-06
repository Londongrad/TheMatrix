import { NavLink } from "react-router-dom";
import "@styles/shared/sidebar.css";

const buildLinkClass = ({ isActive }: { isActive: boolean }) =>
  "sidebar-link" + (isActive ? " sidebar-link-active" : "");

const Sidebar = () => {
  return (
    <aside className="sidebar">
      <div className="sidebar-title">The Matrix</div>
      <nav className="sidebar-nav">
        <NavLink to="/" end className={buildLinkClass}>
          Dashboard
        </NavLink>
        <NavLink to="/incidents" className={buildLinkClass}>
          Incidents
        </NavLink>
        <NavLink to="/systems" className={buildLinkClass}>
          Systems
        </NavLink>
        <NavLink to="/citizens" className={buildLinkClass}>
          Citizens
        </NavLink>
        <NavLink to="/settings" className={buildLinkClass}>
          Settings
        </NavLink>
      </nav>
    </aside>
  );
};

export default Sidebar;
