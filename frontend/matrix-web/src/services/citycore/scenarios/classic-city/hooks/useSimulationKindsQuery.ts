import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getSimulationKinds} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import type {SimulationKindCatalogItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

let simulationKindsCache: SimulationKindCatalogItemView[] | null = null;

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useSimulationKindsQuery() {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<SimulationKindCatalogItemView[]>(
        () => simulationKindsCache ?? [],
    );
    const [isLoading, setIsLoading] = useState(simulationKindsCache === null);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);

            const kinds = await getSimulationKinds(abortController.signal);
            simulationKindsCache = kinds;
            setData(kinds);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load simulation kinds."));
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
