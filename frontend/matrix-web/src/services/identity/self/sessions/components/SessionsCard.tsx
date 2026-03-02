import {useEffect, useMemo, useState} from "react";
import {getSessionHistoryPage} from "@services/identity/api/self/sessions/sessionsApi";
import type {SessionInfo} from "@services/identity/api/self/sessions/sessionsTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {getPageRange} from "@shared/lib/paging/pageRange";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import {formatIpAddress} from "@services/identity/self/shared/utils/formatIpAddress";
import {useSessions} from "../hooks/useSessions";
import "@services/identity/self/sessions/styles/sessions-card.css";

type Props = {
    token: string | null;
    logout?: () => Promise<void>;
    confirm: (options: any) => Promise<boolean>;
};

const ENDED_HISTORY_PAGE_SIZE = 50;
const ENDED_HISTORY_SHOW_ALL_LIMIT = 100;

const SessionsCard = ({token, logout, confirm}: Props) => {
    const [isEndedHistoryOpen, setIsEndedHistoryOpen] = useState(false);
    const [showAllEndedHistory, setShowAllEndedHistory] = useState(false);
    const [historyPageSize, setHistoryPageSize] = useState(ENDED_HISTORY_PAGE_SIZE);

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
        sessionsVersion,
        revokeOne,
        revokeOthers,
        revokeAll,
        isCurrentSession,
    } = useSessions({token, logout, confirm});

    const endedHistoryQuery = usePagedQuery<SessionInfo>(
        getSessionHistoryPage,
        historyPageSize,
        [sessionsVersion, showAllEndedHistory],
        {
            enabled: Boolean(token) && isSessionsOpen && isEndedHistoryOpen,
            initialPage: 1,
            errorMessage: "Failed to load ended session history.",
        },
    );

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

    const activeSessionsCount = sessions.filter((session) => session.isActive).length;
    const otherActiveSessionsCount = sessions.filter(
        (session) => session.isActive && !isCurrentSession(session),
    ).length;

    const historyTotalCount = endedHistoryQuery.data?.totalCount ?? 0;
    const historyTotalPages = endedHistoryQuery.data?.totalPages ?? 1;
    const historyPageNumber = endedHistoryQuery.data?.pageNumber ?? endedHistoryQuery.pageNumber;
    const historyRange = getPageRange(
        historyPageNumber,
        historyPageSize,
        historyTotalCount,
    );
    const canShowAllEndedHistory =
        historyTotalCount > ENDED_HISTORY_PAGE_SIZE &&
        historyTotalCount <= ENDED_HISTORY_SHOW_ALL_LIMIT;
    const shouldShowHistoryPagination = !showAllEndedHistory && historyTotalPages > 1;

    useEffect(() => {
        if (!isEndedHistoryOpen) {
            setShowAllEndedHistory(false);
            setHistoryPageSize(ENDED_HISTORY_PAGE_SIZE);
        }
    }, [isEndedHistoryOpen]);

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

    const endedHistoryItems = useMemo(
        () => endedHistoryQuery.data?.items ?? [],
        [endedHistoryQuery.data],
    );

    const handleToggleShowAll = () => {
        if (showAllEndedHistory) {
            setShowAllEndedHistory(false);
            setHistoryPageSize(ENDED_HISTORY_PAGE_SIZE);
            endedHistoryQuery.setPageNumber(1);
            return;
        }

        const expandedPageSize = Math.max(
            ENDED_HISTORY_PAGE_SIZE,
            endedHistoryQuery.data?.totalCount ?? ENDED_HISTORY_PAGE_SIZE,
        );

        setShowAllEndedHistory(true);
        setHistoryPageSize(expandedPageSize);
        endedHistoryQuery.setPageNumber(1);
    };

    const renderHistoryPagination = (position: "top" | "bottom") => {
        if (!shouldShowHistoryPagination) {
            return null;
        }

        return (
            <div
                className={`settings-session-pagination settings-session-pagination--${position}`}
            >
                <Pagination
                    page={historyPageNumber}
                    totalPages={historyTotalPages}
                    onChange={endedHistoryQuery.setPageNumber}
                />
            </div>
        );
    };

    return (
        <section className="settings-card settings-card--sessions settings-card--span-2">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Sessions</h2>
                    <p className="settings-card-description">
                        Review every signed-in device, keep the current session, and load old access history only when you need it.
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
                                    {endedHistoryQuery.data && (
                                        <span className="settings-session-chip">
                                            Ended history: {historyTotalCount}
                                        </span>
                                    )}
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

                            {otherActiveSessionsCount === 0 && historyTotalCount > 0 ? (
                                <p className="settings-hint">
                                    Other active sessions are already gone. The history below is loaded separately and kept only for audit purposes.
                                </p>
                            ) : null}

                            <div className="settings-session-section">
                                <div className="settings-session-section__header">
                                    <h3 className="settings-session-section__title">Active sessions</h3>
                                    <span className="settings-session-section__meta">
                                        {activeSessionsCount}
                                    </span>
                                </div>

                                {sortedSessions.length > 0 ? (
                                    <div className="settings-session-list">
                                        {sortedSessions.map(renderSessionCard)}
                                    </div>
                                ) : (
                                    <p className="settings-hint">
                                        No active sessions were returned. This usually means the current access token is living through a very narrow refresh edge case.
                                    </p>
                                )}
                            </div>

                            <div className="settings-session-section">
                                <div className="settings-session-section__header">
                                    <div className="settings-session-section__titleRow">
                                        <h3 className="settings-session-section__title">Ended session history</h3>
                                        {endedHistoryQuery.data && (
                                            <span className="settings-session-section__meta">
                                                {historyTotalCount}
                                            </span>
                                        )}
                                    </div>

                                    <button
                                        type="button"
                                        className="settings-button settings-button--secondary"
                                        onClick={() => setIsEndedHistoryOpen((value) => !value)}
                                    >
                                        {isEndedHistoryOpen ? "Hide history" : "Show history"}
                                    </button>
                                </div>

                                {!isEndedHistoryOpen ? (
                                    <p className="settings-hint">
                                        Ended sessions are fetched only when you open history, so the screen stays fast even if the audit tail is large.
                                    </p>
                                ) : endedHistoryQuery.isLoading ? (
                                    <div className="settings-session-skeleton">
                                        <div className="settings-session-skeleton-line"/>
                                        <div className="settings-session-skeleton-line"/>
                                    </div>
                                ) : endedHistoryQuery.error ? (
                                    <div className="settings-alert settings-alert--error">
                                        {endedHistoryQuery.error}
                                    </div>
                                ) : historyTotalCount === 0 ? (
                                    <p className="settings-hint">
                                        No ended sessions yet.
                                    </p>
                                ) : (
                                    <>
                                        <div className="settings-session-section__controls">
                                            <div className="settings-session-meta">
                                                <span className="settings-session-chip">
                                                    {showAllEndedHistory
                                                        ? `Showing all ${historyTotalCount} ended sessions`
                                                        : `Showing ${historyRange.start}-${historyRange.end} of ${historyTotalCount}`}
                                                </span>
                                            </div>

                                            {canShowAllEndedHistory && (
                                                <div className="settings-actions-row settings-actions-row--sessions">
                                                    <button
                                                        type="button"
                                                        className="settings-button settings-button--secondary"
                                                        onClick={handleToggleShowAll}
                                                    >
                                                        {showAllEndedHistory ? "Paginate history" : "Show all"}
                                                    </button>
                                                </div>
                                            )}
                                        </div>

                                        {renderHistoryPagination("top")}

                                        <div className="settings-session-list">
                                            {endedHistoryItems.map(renderSessionCard)}
                                        </div>

                                        {renderHistoryPagination("bottom")}
                                    </>
                                )}
                            </div>
                        </>
                    )}
                </div>
            )}
        </section>
    );
};

export default SessionsCard;
