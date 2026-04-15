import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCityActiveTrips} from "@services/simulationcore/scenarios/classic-city/api/citiesApi";
import type {CityActiveTripView} from "@services/simulationcore/scenarios/classic-city/contracts/worldContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityActiveTrips(cityId: string, refetchIntervalMs = 15000) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityActiveTripView[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!cityId) {
            setData([]);
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

            const trips = await getCityActiveTrips(cityId, abortController.signal);

            if (abortController.signal.aborted) {
                return;
            }

            setData(trips);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load active city trips."));
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
            if (document.visibilityState !== "visible") {
                return;
            }

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
            isRefreshing: isLoading && data.length > 0,
            error,
            refetch: load,
        }),
        [data, error, isLoading, load],
    );
}
