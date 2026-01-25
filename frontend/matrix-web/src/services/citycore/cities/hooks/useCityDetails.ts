import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCity} from "@services/citycore/cities/api/citiesApi";
import type {CityView} from "@services/citycore/cities/contracts/citiesContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityDetails(cityId: string) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityView | null>(null);
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

            const city = await getCity(cityId, abortController.signal);
            setData(city);
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(error, "Failed to load city details."));
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
