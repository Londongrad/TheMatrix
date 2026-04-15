import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCityDistrictInfrastructure} from "@services/simulationcore/scenarios/classic-city/api/citiesApi";
import type {CityDistrictInfrastructureView} from "@services/simulationcore/scenarios/classic-city/contracts/infrastructureContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityDistrictInfrastructure(cityId: string, refetchIntervalMs = 30000) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityDistrictInfrastructureView | null>(null);
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

            const infrastructure = await getCityDistrictInfrastructure(cityId, abortController.signal);

            if (abortController.signal.aborted) {
                return;
            }

            setData(infrastructure);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load district infrastructure."));
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
            isRefreshing: isLoading && data !== null,
            error,
            refetch: load,
        }),
        [data, error, isLoading, load],
    );
}
