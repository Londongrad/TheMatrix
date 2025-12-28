import { useEffect, useState } from "react";
import MatrixBackdrop from "@shared/ui/backgrounds/BackgroundRain/MatrixRainBackground";
import Sidebar from "@shared/navigation/Sidebar/Sidebar";
import Topbar from "@shared/navigation/Topbar/Topbar";
import type { NavItem } from "@shared/navigation/Sidebar/types";
import { ChevronRight } from "lucide-react";
import "./shell-layout.css";

export default function MatrixShellLayout({
  title,
  items,
  children,
  storageKey = "mx.sidebar.collapsed",
  onBack,
  brandRight,
  topbarTitle = "Matrix control panel",
  topbarSubtitle = "Live operations overview",
}: {
  title: string;
  items: NavItem[];
  children: React.ReactNode;
  storageKey?: string;
  onBack?: () => void;
  brandRight?: React.ReactNode;
  topbarTitle?: string;
  topbarSubtitle?: string;
}) {
  const [collapsed, setCollapsed] = useState(() => {
    try {
      return localStorage.getItem(storageKey) === "1";
    } catch {
      return false;
    }
  });

  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    try {
      localStorage.setItem(storageKey, collapsed ? "1" : "0");
    } catch {}
  }, [collapsed, storageKey]);

  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth >= 980) setMobileOpen(false);
    };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);

  return (
    <div
      className={`mx-shell${collapsed ? " is-collapsed" : ""}${
        mobileOpen ? " is-mobile-open" : ""
      }`}
    >
      <MatrixBackdrop rainOpacity={0.4} />
      <div className="mx-shell__overlay" onClick={() => setMobileOpen(false)} />

      <aside className="mx-shell__sidebar">
        <div className="mx-shell__sidebarPanel">
          <Sidebar
            title={title}
            items={items}
            onNavigate={() => setMobileOpen(false)}
            onBack={onBack}
            brandRight={brandRight}
            onCollapse={!collapsed ? () => setCollapsed(true) : undefined}
          />
        </div>

        {collapsed && (
          <button
            type="button"
            className="mx-shell__handle"
            onClick={() => setCollapsed(false)}
            aria-label="Expand sidebar"
            title="Expand"
          >
            <ChevronRight size={18} />
          </button>
        )}
      </aside>

      <div className="mx-shell__main">
        <header className="mx-shell__header">
          <button
            type="button"
            className="mx-shell__burger"
            onClick={() => setMobileOpen(true)}
            aria-label="Open menu"
            title="Menu"
          >
            ☰
          </button>
          <div className="mx-shell__topbar">
            <Topbar title={topbarTitle} subtitle={topbarSubtitle} />
          </div>
        </header>

        <main className="mx-shell__content">{children}</main>
      </div>
    </div>
  );
}
