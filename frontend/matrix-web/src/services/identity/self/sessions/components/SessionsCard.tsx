import {useEffect, useMemo, useState} from "react";
import type {SessionInfo} from "@services/identity/api/self/sessions/sessionsTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {getPageRange} from "@shared/lib/paging/pageRange";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import {useSessions} from "../hooks/useSessions";
import "@services/identity/self/sessions/styles/sessions-card.css";

type Props = {
    token: string | null;
    logout?: () => Promise<void>;
    confirm: (options: any) => Promise<boolean>;
};

const ENDED_HISTORY_PAGE_SIZE = 50;

const SessionsCard = ({token, logout, confirm}: Props) => {
    const [isEndedHistoryOpen, setIsEndedHistoryOpen] = useState(false);
    const [endedHistoryPage, setEndedHistoryPage] = useState(1);
    const [showAllEndedHistory, setShowAllEndedHistory] = useState(false);

    const {
        isSessionsOpen,
        setIsSessionsOpen,
        sessions,
        sortedSessions,
        sessionsError,
        isLoadingSessions,
        revokingSessionId,
        isRevokingAll,
        isRevokingOther,
        loadSessions,
        revokeOne,
        revokeOthers,
        revokeAll,
        isCurrentSession,
    } = useSessions({token, logout, confirm});

    const buildLocation = (session: SessionInfo) => {
        if (session.location) {
            return session.location;
        }

        const parts = [session.city, session.region, session.country].filter(Boolean) as string[];
        return parts.length ? parts.join(", ") : "";
    };

    const fmtUtc = (value?: string | null) => {
        if (!value) {
            return "";
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
    };

    const formatIpAddress = (value?: string | null) => {
        if (!value) {
            return "";
        }

        const normalized = value.trim().toLowerCase();
        if (
            normalized === "127.0.0.1" ||
            normalized === "::1" ||
            normalized === "::ffff:127.0.0.1"
        ) {
            return "localhost";
        }

        return value;
    };

    const activeSessionsCount = sessions.filter((session) => session.isActive).length;
    const otherActiveSessionsCount = sessions.filter(
        (session) => session.isActive && !isCurrentSession(session),
    ).length;
    const endedSessionsCount = sessions.length - activeSessionsCount;

    const activeSessions = sortedSessions.filter((session) => session.isActive);
    const endedSessions = sortedSessions.filter((session) => !session.isActive);
    const endedHistoryTotalPages = Math.max(
        1,
        Math.ceil(endedSessionsCount / ENDED_HISTORY_PAGE_SIZE),
    );
    const endedHistoryRange = getPageRange(
        endedHistoryPage,
        ENDED_HISTORY_PAGE_SIZE,
        endedSessionsCount,
    );

    const visibleEndedSessions = useMemo(() => {
        if (showAllEndedHistory) {
            return endedSessions;
        }

        const startIndex = (endedHistoryPage - 1) * ENDED_HISTORY_PAGE_SIZE;
        return endedSessions.slice(startIndex, startIndex + ENDED_HISTORY_PAGE_SIZE);
    }, [endedHistoryPage, endedSessions, showAllEndedHistory]);

    useEffect(() => {
        if (endedHistoryPage > endedHistoryTotalPages) {
            setEndedHistoryPage(endedHistoryTotalPages);
        }
    }, [endedHistoryPage, endedHistoryTotalPages]);

    useEffect(() => {
        if (showAllEndedHistory) {
            setEndedHistoryPage(1);
        }
    }, [showAllEndedHistory]);

    const renderSessionCard = (session: SessionInfo) => {
        const location = buildLocation(session);
        const current = isCurrentSession(session);
        const ipAddress = formatIpAddress(session.ipAddress);

        return (
            <div
                key={session.id}
                className={`settings-session-item ${
                    current ? "settings-session-item--current" : ""
                } ${
                    !session.isActive ? "settings-session-item--ended" : ""
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
                                {session.isActive ? "Active" : "Ended"}
                            </span>
                        )}
                    </div>

                    <div className="settings-session-meta">
                        {ipAddress && (
                            <span className="settings-session-chip">
                                IP: {ipAddress}
                            </span>
                        )}

                        {location && (
                            <span className="settings-session-chip">
                                {location}
                            </span>
                        )}

                        <span className="settings-session-chip">
                            {session.isPersistent ? "Persistent sign-in" : "Session sign-in"}
                        </span>

                        <span className="settings-session-chip">
                            {session.lastUsedAtUtc
                                ? `Last used: ${fmtUtc(session.lastUsedAtUtc)}`
                                : `Created: ${fmtUtc(session.createdAtUtc)}`}
                        </span>

                        <span className="settings-session-chip">
                            Expires: {fmtUtc(session.refreshTokenExpiresAtUtc)}
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

                {session.isActive && (
                    <div className="settings-session-actions">
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeSessionsRevoke}
                            displayMode="disable"
                        >
                            <button
                                type="button"
                                className="settings-button settings-button--ghost-danger"
                                onClick={() => void revokeOne(session)}
                                disabled={revokingSessionId === session.id || isRevokingAll}
                            >
                                {revokingSessionId === session.id
                                    ? "Revoking..."
                                    : current
                                        ? "Log out"
                                        : "Revoke"}
                            </button>
                        </RequirePermission>
                    </div>
                )}
            </div>
        );
    };

    return (
        <section className="settings-card settings-card--sessions settings-card--span-2">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Sessions</h2>
                    <p className="settings-card-description">
                        Review every signed-in device, keep the current session, and inspect old access history without losing it.
                    </p>
                </div>

                <div className="settings-header-actions">
                    {isSessionsOpen && (
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeSessionsRead}
                            displayMode="disable"
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
                        <div className="settings-sessions-summary">
                            <p className="settings-muted">
                                Sessions are hidden. Click <b>Show sessions</b> to load and manage them.
                            </p>
                            {sessions.length > 0 && (
                                <div className="settings-session-meta">
                                    <span className="settings-session-chip">
                                        Active: {activeSessionsCount}
                                    </span>
                                    <span className="settings-session-chip">
                                        Other active: {otherActiveSessionsCount}
                                    </span>
                                    <span className="settings-session-chip">
                                        Ended history: {endedSessionsCount}
                                    </span>
                                </div>
                            )}
                        </div>
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
                        <>
                            <div className="settings-sessions-toolbar">
                                <div className="settings-session-meta">
                                    <span className="settings-session-chip">
                                        Active: {activeSessionsCount}
                                    </span>
                                    <span className="settings-session-chip">
                                        Other active: {otherActiveSessionsCount}
                                    </span>
                                    <span className="settings-session-chip">
                                        Ended history: {endedSessionsCount}
                                    </span>
                                </div>

                                <div className="settings-actions-row settings-actions-row--sessions">
                                    <RequirePermission
                                        perm={PermissionKeys.IdentityMeSessionsRevokeAll}
                                        displayMode="disable"
                                    >
                                        <button
                                            type="button"
                                            className="settings-button settings-button--secondary"
                                            onClick={() => void revokeOthers()}
                                            disabled={
                                                !token ||
                                                isRevokingOther ||
                                                isRevokingAll ||
                                                isLoadingSessions ||
                                                otherActiveSessionsCount === 0
                                            }
                                            title={
                                                otherActiveSessionsCount === 0
                                                    ? "There are no other active sessions to revoke."
                                                    : undefined
                                            }
                                        >
                                            {isRevokingOther ? "Revoking..." : "Revoke other active sessions"}
                                        </button>
                                    </RequirePermission>

                                    <RequirePermission
                                        perm={PermissionKeys.IdentityMeSessionsRevokeAll}
                                        displayMode="disable"
                                    >
                                        <button
                                            type="button"
                                            className="settings-button settings-button--danger-outline"
                                            onClick={() => void revokeAll()}
                                            disabled={!token || isRevokingAll || isRevokingOther || isLoadingSessions}
                                        >
                                            {isRevokingAll ? "Revoking..." : "Revoke all active sessions"}
                                        </button>
                                    </RequirePermission>
                                </div>
                            </div>

                            {otherActiveSessionsCount === 0 && endedSessionsCount > 0 ? (
                                <p className="settings-hint">
                                    Other entries are already ended. They remain visible as access history, so
                                    there is nothing left for <b>Revoke other active sessions</b> to revoke.
                                </p>
                            ) : null}

                            <div className="settings-session-section">
                                <div className="settings-session-section__header">
                                    <h3 className="settings-session-section__title">Active sessions</h3>
                                    <span className="settings-session-section__meta">
                                        {activeSessionsCount}
                                    </span>
                                </div>

                                <div className="settings-session-list">
                                    {activeSessions.map(renderSessionCard)}
                                </div>
                            </div>

                            {endedSessionsCount > 0 && (
                                <div className="settings-session-section">
                                    <div className="settings-session-section__header">
                                        <div className="settings-session-section__titleRow">
                                            <h3 className="settings-session-section__title">Ended session history</h3>
                                            <span className="settings-session-section__meta">
                                                {endedSessionsCount}
                                            </span>
                                        </div>

                                        <button
                                            type="button"
                                            className="settings-button settings-button--secondary"
                                            onClick={() => setIsEndedHistoryOpen((value) => !value)}
                                        >
                                            {isEndedHistoryOpen ? "Hide history" : "Show history"}
                                        </button>
                                    </div>

                                    {isEndedHistoryOpen ? (
                                        <>
                                            <div className="settings-session-section__controls">
                                                <div className="settings-session-meta">
                                                    <span className="settings-session-chip">
                                                        {showAllEndedHistory
                                                            ? `Showing all ${endedSessionsCount} ended sessions`
                                                            : `Showing ${endedHistoryRange.start}-${endedHistoryRange.end} of ${endedSessionsCount}`}
                                                    </span>
                                                </div>

                                                {endedSessionsCount > ENDED_HISTORY_PAGE_SIZE && (
                                                    <div className="settings-actions-row settings-actions-row--sessions">
                                                        <button
                                                            type="button"
                                                            className="settings-button settings-button--secondary"
                                                            onClick={() => setShowAllEndedHistory((value) => !value)}
                                                        >
                                                            {showAllEndedHistory ? "Paginate history" : "Show all"}
                                                        </button>
                                                    </div>
                                                )}
                                            </div>

                                            <div className="settings-session-list">
                                                {visibleEndedSessions.map(renderSessionCard)}
                                            </div>

                                            {!showAllEndedHistory && endedSessionsCount > ENDED_HISTORY_PAGE_SIZE && (
                                                <Pagination
                                                    page={endedHistoryPage}
                                                    totalPages={endedHistoryTotalPages}
                                                    onChange={setEndedHistoryPage}
                                                />
                                            )}
                                        </>
                                    ) : (
                                        <p className="settings-hint">
                                            Ended sessions stay here as audit history. Expand this section only when
                                            you need to inspect old devices or previous sign-ins.
                                        </p>
                                    )}
                                </div>
                            )}
                        </>
                    )}
                </div>
            )}
        </section>
    );
};

export default SessionsCard;
