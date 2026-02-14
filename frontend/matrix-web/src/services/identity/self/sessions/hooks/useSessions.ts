import {useEffect, useMemo, useState} from "react";
import type {SessionInfo} from "@services/identity/api/self/sessions/sessionsTypes.ts";
import {getSessions, revokeAllSessions, revokeSession} from "@services/identity/api/self/sessions/sessionsApi";
import {getOrCreateDeviceId} from "@services/identity/api/self/auth/deviceInfo";

type ConfirmFn = (options: any) => Promise<boolean>;

export function useSessions(options: {
    token: string | null;
    logout?: () => Promise<void>;
    confirm: ConfirmFn;
}) {
    const {token, logout, confirm} = options;

    const currentDeviceId = useMemo(() => getOrCreateDeviceId(), []);

    const [isSessionsOpen, setIsSessionsOpen] = useState(false);
    const [sessions, setSessions] = useState<SessionInfo[]>([]);
    const [sessionsError, setSessionsError] = useState<string | null>(null);
    const [isLoadingSessions, setIsLoadingSessions] = useState(false);
    const [revokingSessionId, setRevokingSessionId] = useState<string | null>(null);
    const [isRevokingAll, setIsRevokingAll] = useState(false);

    const safeConfirm = async (cfg: any): Promise<boolean> => {
        try {
            return await confirm(cfg);
        } catch {
            const text =
                typeof cfg === "string" ? cfg : cfg?.description || "Are you sure?";
            return window.confirm(text);
        }
    };

    const loadSessions = async () => {
        if (!token) {
            setSessions([]);
            setSessionsError(null);
            return;
        }

        try {
            setIsLoadingSessions(true);
            setSessionsError(null);

            const list = await getSessions();
            setSessions(list ?? []);
        } catch (err: any) {
            console.error(err);
            setSessionsError(err?.message || "Failed to load sessions.");
        } finally {
            setIsLoadingSessions(false);
        }
    };

    useEffect(() => {
        if (isSessionsOpen) {
            void loadSessions();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [isSessionsOpen, token]);

    const currentSessionId = useMemo(() => {
        const currentByServer = sessions.find((s) => s.isCurrent);
        if (currentByServer) {
            return currentByServer.id;
        }

        const sameDevice = sessions.filter((s) => s.deviceId === currentDeviceId);
        if (sameDevice.length === 0) {
            return null;
        }

        sameDevice.sort((a, b) => {
            const at = new Date(a.lastUsedAtUtc ?? a.createdAtUtc).getTime();
            const bt = new Date(b.lastUsedAtUtc ?? b.createdAtUtc).getTime();
            return bt - at;
        });

        return sameDevice[0].id;
    }, [sessions, currentDeviceId]);

    const isCurrentSession = (session: SessionInfo) =>
        session.isCurrent || (currentSessionId !== null && session.id === currentSessionId);

    const sortedSessions = useMemo(() => {
        const copy = [...sessions];

        copy.sort((a, b) => {
            const ac = isCurrentSession(a);
            const bc = isCurrentSession(b);

            if (ac && !bc) return -1;
            if (!ac && bc) return 1;

            if (a.isActive && !b.isActive) return -1;
            if (!a.isActive && b.isActive) return 1;

            const at = new Date(a.lastUsedAtUtc ?? a.createdAtUtc).getTime();
            const bt = new Date(b.lastUsedAtUtc ?? b.createdAtUtc).getTime();
            return bt - at;
        });

        return copy;
    }, [sessions, currentSessionId]);

    const doLogoutAndRedirect = async () => {
        try {
            if (typeof logout === "function") {
                await logout();
            }
        } finally {
            window.location.href = "/login";
        }
    };

    const revokeOne = async (session: SessionInfo) => {
        if (!token) {
            setSessionsError("You are not authenticated.");
            return;
        }

        const confirmed = await safeConfirm({
            title: isCurrentSession(session)
                ? "Log out from this session"
                : "Revoke session",
            description: isCurrentSession(session)
                ? "This is your current session. Revoking it will immediately log you out on this device."
                : "This device will be signed out and will need to log in again.",
            confirmText: isCurrentSession(session) ? "Log out" : "Revoke",
            cancelText: "Cancel",
            tone: "danger",
        });

        if (!confirmed) return;

        try {
            setRevokingSessionId(session.id);
            setSessionsError(null);

            await revokeSession(session.id);

            if (isCurrentSession(session)) {
                await doLogoutAndRedirect();
                return;
            }

            await loadSessions();
        } catch (err: any) {
            console.error(err);
            setSessionsError(err?.message || "Failed to revoke session.");
        } finally {
            setRevokingSessionId(null);
        }
    };

    const revokeAll = async () => {
        if (!token) {
            setSessionsError("You are not authenticated.");
            return;
        }

        const confirmed = await safeConfirm({
            title: "Revoke all sessions",
            description:
                "Every session will be revoked, including the current one. You will be signed out on this device too.",
            confirmText: "Revoke all",
            cancelText: "Cancel",
            tone: "danger",
        });

        if (!confirmed) return;

        try {
            setIsRevokingAll(true);
            setSessionsError(null);

            await revokeAllSessions();
            await doLogoutAndRedirect();
        } catch (err: any) {
            console.error(err);
            setSessionsError(err?.message || "Failed to revoke all sessions.");
        } finally {
            setIsRevokingAll(false);
        }
    };

    return {
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
    };
}
