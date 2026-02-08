import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getSimulationClock} from "@services/citycore/simulation/api/simulationApi";
import type {SimulationView} from "@services/citycore/simulation/contracts/simulationContracts";

export interface SimulationClockSyncMeta {
    requestedAtMs: number;
    receivedAtMs: number;
}

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useSimulationClock(simulationId: string, refetchIntervalMs = 5000) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<SimulationView | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [syncMeta, setSyncMeta] = useState<SimulationClockSyncMeta | null>(null);

    const load = useCallback(async () => {
        if (!simulationId) {
            setData(null);
            setError(null);
            setIsLoading(false);
            setSyncMeta(null);
            return;
        }

        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;
        const requestedAtMs = performance.now();

        try {
            setIsLoading(true);
            setError(null);

            const response = await getSimulationClock(simulationId, abortController.signal);

            if (abortController.signal.aborted) {
                return;
            }

            const receivedAtMs = performance.now();
            setData(response);
            setSyncMeta({
                requestedAtMs,
                receivedAtMs,
            });
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load simulation."));
        } finally {
            if (!abortController.signal.aborted) {
                setIsLoading(false);
            }
        }
    }, [simulationId]);

    useEffect(() => {
        void load();

        return () => {
            abortRef.current?.abort();
        };
    }, [load]);

    useEffect(() => {
        if (!simulationId || refetchIntervalMs <= 0) {
            return;
        }

        const timerId = window.setInterval(() => {
            void load();
        }, refetchIntervalMs);

        return () => {
            window.clearInterval(timerId);
        };
    }, [simulationId, load, refetchIntervalMs]);

    return useMemo(
        () => ({
            data,
            isLoading,
            error,
            syncMeta,
            refetch: load,
        }),
        [data, error, isLoading, load, syncMeta],
    );
}
