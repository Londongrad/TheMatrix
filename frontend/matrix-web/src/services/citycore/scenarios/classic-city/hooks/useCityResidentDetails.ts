import {useCallback, useEffect, useMemo, useRef, useState} from "react";
import {getCityResidentDetails} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import type {CityResidentDetailsDto} from "@services/population/person/api/personTypes";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityResidentDetails(
    cityId: string,
    residentId: string,
    enabled = true,
) {
    const abortRef = useRef<AbortController | null>(null);

    const [data, setData] = useState<CityResidentDetailsDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!enabled || !cityId || !residentId) {
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

            const resident = await getCityResidentDetails(cityId, residentId, abortController.signal);
            setData(resident);
        } catch (queryError: unknown) {
            if (abortController.signal.aborted) {
                return;
            }

            setError(getErrorMessage(queryError, "Failed to load resident details."));
        } finally {
            if (!abortController.signal.aborted) {
                setIsLoading(false);
            }
        }
    }, [cityId, enabled, residentId]);

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
