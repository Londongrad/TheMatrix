import { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@api/identity/auth/AuthContext";
import "@styles/shared/topbar.css";

const Topbar = () => {
  const { user, logout } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const navigate = useNavigate();
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
    navigate("/userSettings");
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
    <header className="topbar">
      <div className="topbar-left">
        <span className="topbar-caption">City control panel</span>
      </div>

      <div className="topbar-right">
        <div className="topbar-user" ref={menuRef}>
          <button
            type="button"
            className={`user-menu-toggle ${
              isOpen ? "user-menu-toggle--open" : ""
            }`}
            onClick={handleToggle}
          >
            <div className="user-avatar">
              {user?.avatarUrl ? (
                <img
                  src={user.avatarUrl}
                  alt={displayName}
                  className="user-avatar-image"
                  draggable={false}
                />
              ) : (
                initial
              )}
            </div>
            <div className="user-text">
              <span className="user-name">{displayName}</span>
            </div>
            <span
              className={`user-caret ${isOpen ? "open" : ""}`}
              aria-hidden="true"
            >
              <svg className="user-caret-icon" viewBox="0 0 20 20" fill="none">
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
            <div className="user-dropdown">
              <button
                type="button"
                className="user-dropdown-item"
                onClick={handleGoToSettings}
              >
                Settings
              </button>
              <button
                type="button"
                className="user-dropdown-item logout"
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
};

export default Topbar;
