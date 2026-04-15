import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCityMapTopology} from "@services/simulationcore/scenarios/classic-city/api/citiesApi";
import type {CityMapTopologyView} from "@services/simulationcore/scenarios/classic-city/contracts/worldContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityMapTopology(cityId: string) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityMapTopologyView | null>(null);
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

            const topology = await getCityMapTopology(cityId, abortController.signal);
            setData(topology);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load city map topology."));
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
