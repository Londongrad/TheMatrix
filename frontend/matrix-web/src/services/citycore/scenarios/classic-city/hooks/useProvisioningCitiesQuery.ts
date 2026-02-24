import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getProvisioningCities} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

const provisioningCitiesCacheKey = "provisioning";
const cityListCache = new Map<string, CityListItemView[]>();

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useProvisioningCitiesQuery() {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityListItemView[]>(
        () => cityListCache.get(provisioningCitiesCacheKey) ?? [],
    );
    const [isLoading, setIsLoading] = useState(!cityListCache.has(provisioningCitiesCacheKey));
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        abortRef.current?.abort();

        const abortController = new AbortController();
        abortRef.current = abortController;

        try {
            setIsLoading(true);
            setError(null);

            const cities = await getProvisioningCities(abortController.signal);
            cityListCache.set(provisioningCitiesCacheKey, cities);
            setData(cities);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load provisioning queue."));
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
