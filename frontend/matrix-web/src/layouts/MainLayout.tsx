import React, { type PropsWithChildren } from "react";
import Sidebar from "../components/Sidebar";
import Topbar from "../components/Topbar";

const MainLayout: React.FC<PropsWithChildren> = ({ children }) => {
  return (
    <div className="app-root">
      <Sidebar />
      <div className="app-main">
        <Topbar />
        <main className="app-content">{children}</main>
      </div>
    </div>
  );
};

export default MainLayout;
