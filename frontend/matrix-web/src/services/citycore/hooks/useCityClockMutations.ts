import { useState } from "react";
import {
    bootstrapCity,
    jump,
    pause,
    resume,
    setSpeed,
} from "@services/citycore/api/cityCoreApi";

interface MutateOptions {
    onSuccess?: () => Promise<void> | void;
    onBootstrapSuccess?: (cityId: string) => void;
}

interface UseCityClockMutationsResult {
    isBootstrapping: boolean;
    isPausing: boolean;
    isResuming: boolean;
    isSettingSpeed: boolean;
    isJumping: boolean;
    actionError: string | null;
    bootstrap: () => Promise<void>;
    pauseClock: () => Promise<void>;
    resumeClock: () => Promise<void>;
    setClockSpeed: (multiplier: number) => Promise<void>;
    jumpClock: (newSimTimeUtc: string) => Promise<void>;
    clearActionError: () => void;
}

export function useCityClockMutations(
    cityId: string,
    token: string | null,
    options: MutateOptions = {},
): UseCityClockMutationsResult {
    const [isBootstrapping, setIsBootstrapping] = useState(false);
    const [isPausing, setIsPausing] = useState(false);
    const [isResuming, setIsResuming] = useState(false);
    const [isSettingSpeed, setIsSettingSpeed] = useState(false);
    const [isJumping, setIsJumping] = useState(false);
    const [actionError, setActionError] = useState<string | null>(null);

    const runAction = async (
        action: () => Promise<void>,
        setBusy: (value: boolean) => void,
        errorMessage: string,
        requiresCityId = true,
    ) => {
        if (!token) {
            setActionError("Your session has expired. Please sign in again.");
            return;
        }

        if (requiresCityId && !cityId) {
            setActionError("Bootstrap a city before running simulation actions.");
            return;
        }

        try {
            setBusy(true);
            setActionError(null);
            await action();
            await options.onSuccess?.();
        } catch (e) {
            console.error(e);
            setActionError(errorMessage);
        } finally {
            setBusy(false);
        }
    };

    return {
        isBootstrapping,
        isPausing,
        isResuming,
        isSettingSpeed,
        isJumping,
        actionError,
        bootstrap: async () => {
            await runAction(
                async () => {
                    const response = await bootstrapCity(token!);
                    options.onBootstrapSuccess?.(response.cityId);
                },
                setIsBootstrapping,
                "Failed to bootstrap city clock.",
                false,
            );
        },
        pauseClock: async () => {
            await runAction(
                () => pause(cityId, token!),
                setIsPausing,
                "Failed to pause simulation.",
            );
        },
        resumeClock: async () => {
            await runAction(
                () => resume(cityId, token!),
                setIsResuming,
                "Failed to resume simulation.",
            );
        },
        setClockSpeed: async (multiplier: number) => {
            await runAction(
                () => setSpeed(cityId, { multiplier }, token!),
                setIsSettingSpeed,
                "Failed to update simulation speed.",
            );
        },
        jumpClock: async (newSimTimeUtc: string) => {
            await runAction(
                () => jump(cityId, { newSimTimeUtc }, token!),
                setIsJumping,
                "Failed to jump simulation clock.",
            );
        },
        clearActionError: () => setActionError(null),
    };
}
