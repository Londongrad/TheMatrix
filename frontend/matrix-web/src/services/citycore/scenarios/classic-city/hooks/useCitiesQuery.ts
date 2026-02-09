import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCities} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

const citiesCache = new Map<string, CityListItemView[]>();

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCitiesQuery(includeArchived: boolean) {
    const cacheKey = String(includeArchived);
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityListItemView[]>(
        () => citiesCache.get(cacheKey) ?? [],
    );
    const [isLoading, setIsLoading] = useState(!citiesCache.has(cacheKey));
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);

            const cities = await getCities(includeArchived, abortController.signal);
            citiesCache.set(cacheKey, cities);
            setData(cities);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load cities."));
        } finally {
            if (!abortController.signal.aborted) {
                setIsLoading(false);
            }
        }
    }, [cacheKey, includeArchived]);

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
