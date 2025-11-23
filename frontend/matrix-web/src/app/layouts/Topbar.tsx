import React from "react";
import "../../styles/shared/topbar.css";

const Topbar: React.FC = () => {
  return (
    <header className="topbar">
      <div className="topbar-left">
        <span className="topbar-caption">City control panel</span>
      </div>
      <div className="topbar-right">
        <span className="topbar-user">God-admin</span>
      </div>
    </header>
  );
};

export default Topbar;
