import {NavLink, useLocation, useResolvedPath} from "react-router-dom";
import type {NavItem} from "./types";
import {ArrowLeft, ChevronLeft} from "lucide-react";
import {IconLock} from "@shared/ui/icons/icons";
import {useWorkspacePreferences} from "@shared/theme/workspacePreferences";
import "./sidebar.css";

function SidebarNavLink({
                            item,
                            onNavigate,
                        }: {
    item: NavItem;
    onNavigate?: () => void;
}) {
    const location = useLocation();
    const resolvedPath = useResolvedPath(item.to);

    const isSearchAwareActive = ({isActive}: { isActive: boolean }) => {
        if (!resolvedPath.search) {
            return isActive;
        }

        const pathMatches = item.end
            ? location.pathname === resolvedPath.pathname
            : location.pathname === resolvedPath.pathname ||
            location.pathname.startsWith(`${resolvedPath.pathname}/`);

        return pathMatches && location.search === resolvedPath.search;
    };

    return (
        <NavLink
            to={item.to}
            end={item.end}
            state={item.getState ? item.getState(location.pathname) : undefined}
            className={(state) =>
                `mx-sb__item${isSearchAwareActive(state) ? " is-active" : ""}`
            }
            onClick={onNavigate}
            title={item.label}
        >
            {item.icon ? <span className="mx-sb__icon">{item.icon}</span> : null}
            <span className="mx-sb__label">{item.label}</span>
            <span className="mx-sb__glow" aria-hidden="true"/>
        </NavLink>
    );
}

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
    const {preferences} = useWorkspacePreferences();
    const backButtonClassName = [
        "mx-sb__markBtn",
        onBack ? "mx-sb__markBtn--active" : "",
        onBack && preferences.animateSidebarBackButton ? "mx-sb__markBtn--animated" : "",
    ]
        .filter(Boolean)
        .join(" ");

    return (
        <div className="mx-sb">
            <div className="mx-sb__brand">
                {/* ЛЕВЫЙ “квадрат” — Back (если передали onBack) */}
                {onBack ? (
                    <button
                        type="button"
                        className={backButtonClassName}
                        onClick={onBack}
                        aria-label="Back"
                        title="Back"
                    >
                        <span className="mx-sb__markBtnIcon" aria-hidden="true">
                            <ArrowLeft size={18}/>
                        </span>
                    </button>
                ) : (
                    <div className="mx-sb__mark" aria-hidden="true"/>
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
                            aria-label="Collapse this menu"
                            title="Collapse this menu"
                        >
                            <ChevronLeft size={18}/>
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
                <IconLock/>
              </span>
                                <span className="mx-sb__glow" aria-hidden="true"/>
                            </div>
                        ) : (
                            <SidebarNavLink
                                key={x.to}
                                item={x}
                                onNavigate={onNavigate}
                            />
                        ),
                )}
            </nav>
        </div>
    );
}
