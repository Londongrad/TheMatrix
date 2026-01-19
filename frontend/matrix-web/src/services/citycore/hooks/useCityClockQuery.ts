import { useCallback, useEffect, useMemo, useState } from "react";
import { getClock } from "@services/citycore/api/cityCoreApi";
import type { CityClockResponseDto } from "@services/citycore/api/cityCoreTypes";
import { HttpError } from "@shared/api/http";

interface UseCityClockQueryOptions {
    enabled?: boolean;
    refetchIntervalMs?: number;
}

export function useCityClockQuery(
    cityId: string,
    token: string | null,
    options: UseCityClockQueryOptions = {},
) {
    const { enabled = true, refetchIntervalMs = 1500 } = options;

    const [data, setData] = useState<CityClockResponseDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);

    const canLoad = enabled && Boolean(token) && cityId.length > 0;

    const fetchClock = useCallback(async () => {
        if (!token || !cityId) return;

        try {
            setIsLoading(true);
            setError(null);
            const response = await getClock(cityId, token);
            setData(response);
            setLastUpdatedAt(new Date());
        } catch (e) {
            console.error(e);
            setData(null);
            if (e instanceof HttpError && e.status === 404) {
                setError("Clock is not initialized for this city yet.");
                return;
            }

            setError("Failed to load simulation clock.");
        } finally {
            setIsLoading(false);
        }
    }, [cityId, token]);

    useEffect(() => {
        if (!canLoad) {
            setData(null);
            setError(null);
            setIsLoading(false);
            setLastUpdatedAt(null);
            return;
        }

        void fetchClock();
    }, [canLoad, fetchClock]);

    useEffect(() => {
        if (!canLoad || refetchIntervalMs <= 0) return;

        const timer = window.setInterval(() => {
            void fetchClock();
        }, refetchIntervalMs);

        return () => {
            window.clearInterval(timer);
        };
    }, [canLoad, fetchClock, refetchIntervalMs]);

    const state = useMemo(
        () => ({ data, isLoading, error, lastUpdatedAt, refetch: fetchClock }),
        [data, isLoading, error, lastUpdatedAt, fetchClock],
    );

    return state;
}
