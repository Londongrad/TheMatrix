//src/services/citycore/simulation/hooks/useSimulationMutations.ts
import { useState } from "react";
import {
    jumpSimulationClock,
    pauseSimulation,
    resumeSimulation,
    setSimulationSpeed,
} from "@services/citycore/simulation/api/simulationApi";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

export function useSimulationMutations() {
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const run = async (action: () => Promise<void>, fallbackMessage: string) => {
        try {
            setIsSubmitting(true);
            setError(null);
            await action();
            return true;
        } catch (error: unknown) {
            setError(getErrorMessage(error, fallbackMessage));
            return false;
        } finally {
            setIsSubmitting(false);
        }
    };

    return {
        isSubmitting,
        error,
        clearError: () => setError(null),
        pause: (cityId: string) =>
            run(() => pauseSimulation(cityId), "Failed to pause simulation."),
        resume: (cityId: string) =>
            run(() => resumeSimulation(cityId), "Failed to resume simulation."),
        setSpeed: (cityId: string, multiplier: number) =>
            run(
                () => setSimulationSpeed(cityId, { multiplier }),
                "Failed to set simulation speed.",
            ),
        jump: (cityId: string, newSimTimeUtc: string) =>
            run(
                () => jumpSimulationClock(cityId, { newSimTimeUtc }),
                "Failed to jump simulation clock.",
            ),
    };
}
