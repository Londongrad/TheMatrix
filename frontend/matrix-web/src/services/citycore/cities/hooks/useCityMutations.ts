import { useState } from "react";
import {
    archiveCity,
    createCity,
    deleteCity,
    renameCity,
} from "@services/citycore/cities/api/citiesApi";
import type {
    CityCreatedView,
    CreateCityRequest,
} from "@services/citycore/cities/contracts/citiesContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityMutations() {
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const run = async <T>(
        action: () => Promise<T>,
        fallbackMessage: string,
    ): Promise<T | null> => {
        try {
            setIsSubmitting(true);
            setError(null);
            return await action();
        } catch (error: unknown) {
            setError(getErrorMessage(error, fallbackMessage));
            return null;
        } finally {
            setIsSubmitting(false);
        }
    };

    return {
        isSubmitting,
        error,
        clearError: () => setError(null),

        create: async (request: CreateCityRequest): Promise<CityCreatedView | null> =>
            run(() => createCity(request), "Failed to create city."),

        rename: async (cityId: string, name: string): Promise<boolean> => {
            const result = await run(
                () => renameCity(cityId, { name }),
                "Failed to rename city.",
            );

            return result !== null;
        },

        archive: async (cityId: string): Promise<boolean> => {
            const result = await run(
                () => archiveCity(cityId),
                "Failed to archive city.",
            );

            return result !== null;
        },

        delete: async (cityId: string): Promise<boolean> => {
            const result = await run(
                () => deleteCity(cityId),
                "Failed to delete city.",
            );

            return result !== null;
        },
    };
}
