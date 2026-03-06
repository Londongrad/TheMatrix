import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCityOperationsDashboard} from "@services/citycore/dashboard/api/dashboardApi";
import type {CityOperationsDashboardView} from "@services/citycore/dashboard/api/dashboardTypes";

let dashboardCache: CityOperationsDashboardView | null = null;

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityOperationsDashboardQuery() {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityOperationsDashboardView | null>(() => dashboardCache);
    const [isLoading, setIsLoading] = useState(!dashboardCache);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);

            const dashboard = await getCityOperationsDashboard(abortController.signal);
            dashboardCache = dashboard;
            setData(dashboard);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load dashboard watchboard."));
        } finally {
            if (!abortController.signal.aborted) {
                setIsLoading(false);
            }
        }
    }, []);

    useEffect(() => {
        void load();

        return () => {
            abortRef.current?.abort();
        };
    }, [load]);

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
