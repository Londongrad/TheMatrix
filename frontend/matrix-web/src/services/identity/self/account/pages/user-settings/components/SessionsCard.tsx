// src/services/identity/account/pages/user-settings/components/SessionsCard.tsx
import type { SessionInfo } from "@services/identity/api/self/sessions/sessionsTypes";
import { useSessions } from "../hooks/useSessions";
import "@services/identity/self/account/styles/sessions-card.css";

type Props = {
  token: string | null;
  logout?: () => Promise<void>;
  confirm: (options: any) => Promise<boolean>;
};

const SessionsCard = ({ token, logout, confirm }: Props) => {
  const {
    isSessionsOpen,
    setIsSessionsOpen,
    sessions,
    sortedSessions,
    sessionsError,
    isLoadingSessions,
    revokingSessionId,
    isRevokingAll,
    loadSessions,
    revokeOne,
    revokeAll,
    isCurrentSession,
  } = useSessions({ token, logout, confirm });

  const buildLocation = (s: SessionInfo) => {
    if (s.location) return s.location;
    const parts = [s.city, s.region, s.country].filter(Boolean) as string[];
    return parts.length ? parts.join(", ") : "";
  };

  const fmtUtc = (value?: string | null) => {
    if (!value) return "";
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? value : d.toLocaleString();
  };

  return (
    <section className="settings-card settings-card--sessions settings-card--span-2">
      <div className="settings-card-header">
        <div>
          <h2 className="settings-card-title">Sessions</h2>
          <p className="settings-card-description">
            Manage active sessions across devices.
          </p>
        </div>

        <div className="settings-header-actions">
          {isSessionsOpen && (
            <button
              type="button"
              className="settings-button settings-button--secondary"
              onClick={() => void loadSessions()}
              disabled={!token || isLoadingSessions || isRevokingAll}
            >
              {isLoadingSessions ? "Loading..." : "Refresh"}
            </button>
          )}

          <button
            type="button"
            className="settings-button settings-button--secondary"
            onClick={() => setIsSessionsOpen((v) => !v)}
            disabled={!token}
          >
            {isSessionsOpen ? "Hide sessions" : "Show sessions"}
          </button>
        </div>
      </div>

      {!isSessionsOpen ? (
        <div className="settings-sessions-summary">
          {!token ? (
            <p className="settings-muted">
              Log in to view and manage sessions.
            </p>
          ) : sessionsError ? (
            <div className="settings-alert settings-alert--error">
              {sessionsError}
            </div>
          ) : (
            <p className="settings-muted">
              Sessions are hidden. Click <b>Show sessions</b> to load and
              manage.
            </p>
          )}
        </div>
      ) : (
        <div className="settings-sessions-body">
          {sessionsError && (
            <div className="settings-alert settings-alert--error">
              {sessionsError}
            </div>
          )}

          {!token ? (
            <p className="settings-muted">
              Log in to view and manage sessions.
            </p>
          ) : isLoadingSessions ? (
            <div className="settings-session-skeleton">
              <div className="settings-session-skeleton-line" />
              <div className="settings-session-skeleton-line" />
              <div className="settings-session-skeleton-line" />
            </div>
          ) : sessions.length === 0 ? (
            <p className="settings-muted">No sessions found.</p>
          ) : (
            <div className="settings-session-list">
              {sortedSessions.map((s) => {
                const location = buildLocation(s);

                return (
                  <div
                    key={s.id}
                    className={`settings-session-item ${
                      isCurrentSession(s)
                        ? "settings-session-item--current"
                        : ""
                    }`}
                  >
                    <div className="settings-session-main">
                      <div className="settings-session-title">
                        <span className="settings-session-device">
                          {s.deviceName}
                        </span>
                        {isCurrentSession(s) && (
                          <span className="settings-pill">Current</span>
                        )}
                      </div>

                      <div className="settings-session-meta">
                        {s.ipAddress && (
                          <span className="settings-session-chip">
                            IP: {s.ipAddress}
                          </span>
                        )}
                        {location && (
                          <span className="settings-session-chip">
                            {location}
                          </span>
                        )}
                        <span className="settings-session-chip">
                          {s.lastUsedAtUtc
                            ? `Last used: ${fmtUtc(s.lastUsedAtUtc)}`
                            : `Created: ${fmtUtc(s.createdAtUtc)}`}
                        </span>
                      </div>

                      <div className="settings-session-ua">{s.userAgent}</div>
                    </div>

                    <div className="settings-session-actions">
                      <button
                        type="button"
                        className="settings-button settings-button--ghost-danger"
                        onClick={() => void revokeOne(s)}
                        disabled={revokingSessionId === s.id || isRevokingAll}
                      >
                        {revokingSessionId === s.id
                          ? "Revoking..."
                          : "Отозвать"}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          <div className="settings-actions-row settings-actions-row--sessions">
            <button
              type="button"
              className="settings-button settings-button--danger-outline"
              onClick={() => void revokeAll()}
              disabled={!token || isRevokingAll || isLoadingSessions}
            >
              {isRevokingAll ? "Revoking..." : "Отозвать все (включая текущую)"}
            </button>
          </div>
        </div>
      )}
    </section>
  );
};

export default SessionsCard;
