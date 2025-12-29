import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import "./topbar.css";
type Props = {
  title: string;
  subtitle?: string;
};

export default function MatrixTopbar({ title, subtitle }: Props) {
  const { user, logout } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const menuRef = useRef<HTMLDivElement | null>(null);

  const displayName =
    (user?.username as string) || (user?.email as string) || "Overseer";

  const initial = displayName.charAt(0).toUpperCase();

  const handleToggle = () => {
    setIsOpen((prev) => !prev);
  };

  const handleLogout = () => {
    setIsOpen(false);
    logout();
    navigate("/login", { replace: true });
  };

  const handleGoToSettings = () => {
    setIsOpen(false);
    navigate("/userSettings/profile", { state: { from: location.pathname } });
  };

  // закрытие по клику вне меню
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (!menuRef.current) return;

      // если кликнули не внутри блока с юзер-меню — закрываем
      if (!menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);
  return (
    <header className="mx-topbar">
      <div className="mx-topbar__left">
        <div className="mx-topbar__title">{title}</div>
        {subtitle ? (
          <div className="mx-topbar__subtitle">{subtitle}</div>
        ) : null}
      </div>

      <div className="mx-topbar__right">
        <div className="mx-topbar__user" ref={menuRef}>
          <button
            type="button"
            className={`mx-topbar__toggle ${
              isOpen ? "mx-topbar__toggle--open" : ""
            }`}
            onClick={handleToggle}
          >
            <div className="mx-topbar__avatar">
              {user?.avatarUrl ? (
                <img
                  src={user.avatarUrl}
                  alt={displayName}
                  className="mx-topbar__avatarImage"
                  draggable={false}
                />
              ) : (
                initial
              )}
            </div>
            <div className="mx-topbar__userText">
              <span className="mx-topbar__userName">{displayName}</span>
            </div>
            <span
              className={`mx-topbar__caret ${
                isOpen ? "mx-topbar__caret--open" : ""
              }`}
              aria-hidden="true"
            >
              <svg className="mx-topbar__caretIcon" viewBox="0 0 20 20">
                <path
                  d="M6 8l4 4 4-4"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
          </button>

          {isOpen && (
            <div className="mx-topbar__dropdown">
              <button
                type="button"
                className="mx-topbar__dropdownItem"
                onClick={handleGoToSettings}
              >
                Settings
              </button>
              <button
                type="button"
                className="mx-topbar__dropdownItem mx-topbar__dropdownItem--danger"
                onClick={handleLogout}
              >
                Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
