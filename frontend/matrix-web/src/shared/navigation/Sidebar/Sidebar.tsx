import { NavLink, useLocation } from "react-router-dom";
import type { NavItem } from "./types";
import { ArrowLeft, ChevronLeft } from "lucide-react";
import { IconLock } from "@shared/ui/icons/icons";
import "./sidebar.css";

export default function MatrixSidebar({
  title,
  items,
  onNavigate,
  onBack,
  onCollapse,
  brandRight,
}: {
  title: string;
  items: NavItem[];
  onNavigate?: () => void;

  onBack?: () => void; // ← Back to app
  onCollapse?: () => void; // ← Collapse sidebar
  brandRight?: React.ReactNode;
}) {
  const location = useLocation();

  return (
    <div className="mx-sb">
      <div className="mx-sb__brand">
        {/* ЛЕВЫЙ “квадрат” — Back (если передали onBack) */}
        {onBack ? (
          <button
            type="button"
            className="mx-sb__markBtn"
            onClick={onBack}
            aria-label="Back to app"
            title="Back to app"
          >
            <ArrowLeft size={18} />
          </button>
        ) : (
          <div className="mx-sb__mark" aria-hidden="true" />
        )}

        <div className="mx-sb__brandText">
          <div className="mx-sb__title">{title}</div>
          <div className="mx-sb__sub">Console</div>
        </div>

        <div className="mx-sb__brandRight">
          {brandRight}

          {/* Collapse показываем только если передали onCollapse */}
          {onCollapse ? (
            <button
              type="button"
              className="mx-sb__iconBtn"
              onClick={onCollapse}
              aria-label="Collapse sidebar"
              title="Collapse"
            >
              <ChevronLeft size={18} />
            </button>
          ) : null}
        </div>
      </div>

      <nav className="mx-sb__nav">
        {items.map((x) =>
          x.disabled ? (
            <div
              key={x.to}
              className="mx-sb__item is-disabled"
              title={x.disabledReason ?? "Недостаточно прав"}
            >
              {x.icon ? <span className="mx-sb__icon">{x.icon}</span> : null}
              <span className="mx-sb__label">{x.label}</span>
              <span className="mx-sb__lock" aria-hidden="true">
                <IconLock />
              </span>
              <span className="mx-sb__glow" aria-hidden="true" />
            </div>
          ) : (
            <NavLink
              key={x.to}
              to={x.to}
              end={x.end}
              state={x.getState ? x.getState(location.pathname) : undefined}
              className={({ isActive }) =>
                `mx-sb__item${isActive ? " is-active" : ""}`
              }
              onClick={onNavigate}
              title={x.label}
            >
              {x.icon ? <span className="mx-sb__icon">{x.icon}</span> : null}
              <span className="mx-sb__label">{x.label}</span>
              <span className="mx-sb__glow" aria-hidden="true" />
            </NavLink>
          ),
        )}
      </nav>
    </div>
  );
}
