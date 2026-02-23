import {useState} from "react";
import {
    createCity,
    retryPopulationBootstrap,
} from "@services/citycore/scenarios/classic-city/api/citiesApi";
import type {
    CityProvisioningView,
    CreateCityRequest,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useCityProvisioning() {
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const run = async (
        action: () => Promise<CityProvisioningView>,
        fallbackMessage: string,
    ): Promise<CityProvisioningView | null> => {
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
        launch: async (request: CreateCityRequest): Promise<CityProvisioningView | null> =>
            run(
                () => createCity(request),
                "Failed to launch city provisioning.",
            ),
        retry: async (cityId: string): Promise<CityProvisioningView | null> =>
            run(
                () => retryPopulationBootstrap(cityId),
                "Failed to retry population bootstrap.",
            ),
    };
}
