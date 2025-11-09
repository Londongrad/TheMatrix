import React from "react";

const Sidebar: React.FC = () => {
  return (
    <aside className="sidebar">
      <div className="sidebar-title">The Matrix</div>
      <nav className="sidebar-nav">
        <button className="sidebar-link sidebar-link-active">Dashboard</button>
        <button className="sidebar-link">Incidents</button>
        <button className="sidebar-link">Systems</button>
        <button className="sidebar-link">Citizens</button>
        <button className="sidebar-link">Settings</button>
      </nav>
    </aside>
  );
};

export default Sidebar;
