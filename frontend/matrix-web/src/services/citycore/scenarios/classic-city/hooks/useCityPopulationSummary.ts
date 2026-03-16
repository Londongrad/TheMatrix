import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {HttpError} from "@shared/api/http";
import {getCityPopulationSummary} from "@services/citycore/scenarios/classic-city/api/populationSummaryApi";
import type {
    CityPopulationSummaryView
} from "@services/citycore/scenarios/classic-city/contracts/populationSummaryContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityPopulationSummary(cityId: string, refetchIntervalMs = 15000) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityPopulationSummaryView | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isUnavailable, setIsUnavailable] = useState(false);

    const load = useCallback(async () => {
        if (!cityId) {
            setData(null);
            setIsLoading(false);
            setError(null);
            setIsUnavailable(false);
            return;
        }

        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);
            setIsUnavailable(false);

            const summary = await getCityPopulationSummary(cityId, abortController.signal);

            if (abortController.signal.aborted) {
                return;
            }

            setData(summary);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            if (error instanceof HttpError && error.status === 404) {
                setData(null);
                setError(null);
                setIsUnavailable(true);
                return;
            }

            setError(getErrorMessage(error, "Failed to load population summary."));
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
            isRefreshing: isLoading && data !== null,
            error,
            isUnavailable,
            refetch: load,
        }),
        [data, error, isLoading, isUnavailable, load],
    );
}
