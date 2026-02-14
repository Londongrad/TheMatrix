import type {SessionInfo} from "@services/identity/api/self/sessions/sessionsTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {useSessions} from "../hooks/useSessions";
import "@services/identity/self/sessions/styles/sessions-card.css";

type Props = {
    token: string | null;
    logout?: () => Promise<void>;
    confirm: (options: any) => Promise<boolean>;
};

const SessionsCard = ({token, logout, confirm}: Props) => {
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
    } = useSessions({token, logout, confirm});

    const buildLocation = (session: SessionInfo) => {
        if (session.location) return session.location;

        const parts = [session.city, session.region, session.country].filter(Boolean) as string[];
        return parts.length ? parts.join(", ") : "";
    };

    const fmtUtc = (value?: string | null) => {
        if (!value) return "";

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
    };

    const getSessionStatus = (session: SessionInfo) => {
        if (isCurrentSession(session) && session.isActive) {
            return "Current";
        }

        return session.isActive ? "Active" : "Ended";
    };

    return (
        <section className="settings-card settings-card--sessions settings-card--span-2">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Sessions</h2>
                    <p className="settings-card-description">
                        Review every signed-in device and revoke the ones you no longer trust.
                    </p>
                </div>

                <div className="settings-header-actions">
                    {isSessionsOpen && (
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeSessionsRead}
                            mode="disable"
                        >
                            <button
                                type="button"
                                className="settings-button settings-button--secondary"
                                onClick={() => void loadSessions()}
                                disabled={!token || isLoadingSessions || isRevokingAll}
                            >
                                {isLoadingSessions ? "Loading..." : "Refresh"}
                            </button>
                        </RequirePermission>
                    )}

                    <button
                        type="button"
                        className="settings-button settings-button--secondary"
                        onClick={() => setIsSessionsOpen((value) => !value)}
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
                            Sessions are hidden. Click <b>Show sessions</b> to load and manage them.
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
                            <div className="settings-session-skeleton-line"/>
                            <div className="settings-session-skeleton-line"/>
                            <div className="settings-session-skeleton-line"/>
                        </div>
                    ) : sessions.length === 0 ? (
                        <p className="settings-muted">No sessions found.</p>
                    ) : (
                        <div className="settings-session-list">
                            {sortedSessions.map((session) => {
                                const location = buildLocation(session);
                                const current = isCurrentSession(session);

                                return (
                                    <div
                                        key={session.id}
                                        className={`settings-session-item ${
                                            current ? "settings-session-item--current" : ""
                                        }`}
                                    >
                                        <div className="settings-session-main">
                                            <div className="settings-session-title">
                                                <span className="settings-session-device">
                                                    {session.deviceName}
                                                </span>

                                                {current ? (
                                                    <span className="settings-pill">Current</span>
                                                ) : (
                                                    <span className="settings-session-status">
                                                        {getSessionStatus(session)}
                                                    </span>
                                                )}
                                            </div>

                                            <div className="settings-session-meta">
                                                {session.ipAddress && (
                                                    <span className="settings-session-chip">
                                                        IP: {session.ipAddress}
                                                    </span>
                                                )}

                                                {location && (
                                                    <span className="settings-session-chip">
                                                        {location}
                                                    </span>
                                                )}

                                                <span className="settings-session-chip">
                                                    {session.lastUsedAtUtc
                                                        ? `Last used: ${fmtUtc(session.lastUsedAtUtc)}`
                                                        : `Created: ${fmtUtc(session.createdAtUtc)}`}
                                                </span>

                                                {!session.isActive && (
                                                    <span className="settings-session-chip">
                                                        Session ended
                                                    </span>
                                                )}
                                            </div>

                                            {current && (
                                                <div className="settings-session-current-note">
                                                    You are using this session right now.
                                                </div>
                                            )}

                                            <div className="settings-session-ua">{session.userAgent}</div>
                                        </div>

                                        <div className="settings-session-actions">
                                            <RequirePermission
                                                perm={PermissionKeys.IdentityMeSessionsRevoke}
                                                mode="disable"
                                            >
                                                <button
                                                    type="button"
                                                    className="settings-button settings-button--ghost-danger"
                                                    onClick={() => void revokeOne(session)}
                                                    disabled={
                                                        revokingSessionId === session.id || isRevokingAll
                                                    }
                                                >
                                                    {revokingSessionId === session.id
                                                        ? "Revoking..."
                                                        : current
                                                            ? "Log out"
                                                            : "Revoke"}
                                                </button>
                                            </RequirePermission>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}

                    <div className="settings-actions-row settings-actions-row--sessions">
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeSessionsRevokeAll}
                            mode="disable"
                        >
                            <button
                                type="button"
                                className="settings-button settings-button--danger-outline"
                                onClick={() => void revokeAll()}
                                disabled={!token || isRevokingAll || isLoadingSessions}
                            >
                                {isRevokingAll ? "Revoking..." : "Revoke all sessions"}
                            </button>
                        </RequirePermission>
                    </div>
                </div>
            )}
        </section>
    );
};

export default SessionsCard;
