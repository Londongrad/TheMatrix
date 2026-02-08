import {useState} from "react";
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
        pause: (simulationId: string) =>
            run(() => pauseSimulation(simulationId), "Failed to pause simulation."),
        resume: (simulationId: string) =>
            run(() => resumeSimulation(simulationId), "Failed to resume simulation."),
        setSpeed: (simulationId: string, multiplier: number) =>
            run(
                () => setSimulationSpeed(simulationId, {multiplier}),
                "Failed to set simulation speed.",
            ),
        jump: (simulationId: string, newSimTimeUtc: string) =>
            run(
                () => jumpSimulationClock(simulationId, {newSimTimeUtc}),
                "Failed to jump simulation clock.",
            ),
    };
}
