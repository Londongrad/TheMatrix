import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { getSimulationClock } from "@services/citycore/simulation/api/simulationApi";
import type { SimulationView } from "@services/citycore/simulation/contracts/simulationContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useSimulationClock(cityId: string, refetchIntervalMs = 5000) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<SimulationView | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!cityId) {
            setData(null);
            setError(null);
            setIsLoading(false);
            return;
        }

        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);

            const response = await getSimulationClock(cityId, abortController.signal);
            setData(response);
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
    }, [cityId]);

    useEffect(() => {
        void load();

        return () => {
            abortRef.current?.abort();
        };
    }, [load]);

    useEffect(() => {
        if (!cityId || refetchIntervalMs <= 0) {
            return;
        }

        const timerId = window.setInterval(() => {
            void load();
        }, refetchIntervalMs);

        return () => {
            window.clearInterval(timerId);
        };
    }, [cityId, load, refetchIntervalMs]);

    return useMemo(
        () => ({
            data,
            isLoading,
            error,
            refetch: load,
        }),
        [data, error, isLoading, load],
    );
}
