import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import ClockDisplay from "@services/citycore/simulation/components/ClockDisplay";
import { useSimulationClock } from "@services/citycore/simulation/hooks/useSimulationClock";
import { useSimulationMutations } from "@services/citycore/simulation/hooks/useSimulationMutations";
import { localDateTimeToUtcIso } from "@services/citycore/cities/utils/dateTime";
import "@services/citycore/simulation/styles/simulation-panel.css";

const DRIFT_SNAP_THRESHOLD_MS = 3000;
const DRIFT_BLEND_FACTOR = 0.2;
const TICK_INTERVAL_MS = 250;

interface SimulationPanelProps {
    cityId: string;
}

const SimulationPanel = ({ cityId }: SimulationPanelProps) => {
    const simulationQuery = useSimulationClock(cityId, 5000);
    const simulationMutations = useSimulationMutations();

    const [localSimMs, setLocalSimMs] = useState<number | null>(null);
    const [customSpeed, setCustomSpeed] = useState("1");
    const [jumpInput, setJumpInput] = useState("");
    const [customSpeedError, setCustomSpeedError] = useState<string | null>(null);
    const [jumpError, setJumpError] = useState<string | null>(null);

    const lastFrameRef = useRef<number | null>(null);

    const clock = simulationQuery.data;
    const isRunning = clock?.state.toLowerCase() === "running";

    useEffect(() => {
        if (!clock) {
            setLocalSimMs(null);
            lastFrameRef.current = null;
            return;
        }

        const serverMs = new Date(clock.simTimeUtc).getTime();

        if (!Number.isFinite(serverMs)) {
            return;
        }

        setLocalSimMs((current) => {
            if (current === null) {
                return serverMs;
            }

            const drift = serverMs - current;

            if (Math.abs(drift) > DRIFT_SNAP_THRESHOLD_MS) {
                return serverMs;
            }

            return current + drift * DRIFT_BLEND_FACTOR;
        });
    }, [clock]);

    useEffect(() => {
        if (!clock || !isRunning || localSimMs === null) {
            lastFrameRef.current = null;
            return;
        }

        const timerId = window.setInterval(() => {
            const now = Date.now();

            setLocalSimMs((current) => {
                if (current === null) {
                    return current;
                }

                const previousFrame = lastFrameRef.current ?? now;
                const elapsedMs = now - previousFrame;
                const speed = Number.isFinite(clock.speed) ? clock.speed : 1;

                return current + elapsedMs * speed;
            });

            lastFrameRef.current = now;
        }, TICK_INTERVAL_MS);

        return () => {
            window.clearInterval(timerId);
        };
    }, [clock, isRunning, localSimMs]);

    const localDate = useMemo(() => {
        return localSimMs === null ? null : new Date(localSimMs);
    }, [localSimMs]);

    const runCommand = async (action: () => Promise<boolean>) => {
        simulationMutations.clearError();
        const isOk = await action();

        if (isOk) {
            await simulationQuery.refetch();
        }
    };

    const onSetCustomSpeed = async (event: FormEvent) => {
        event.preventDefault();

        const speed = Number(customSpeed);

        if (!Number.isFinite(speed)) {
            setCustomSpeedError("Speed must be a valid number.");
            return;
        }

        if (speed <= 0) {
            setCustomSpeedError("Speed must be greater than 0.");
            return;
        }

        setCustomSpeedError(null);
        await runCommand(() => simulationMutations.setSpeed(cityId, speed));
    };

    const onJump = async (event: FormEvent) => {
        event.preventDefault();

        const utcIso = localDateTimeToUtcIso(jumpInput);

        if (!utcIso) {
            setJumpError("Please enter a valid date and time.");
            return;
        }

        setJumpError(null);
        await runCommand(() => simulationMutations.jump(cityId, utcIso));
    };

    return (
        <Card title="Simulation" subtitle="Clock state and controls">
            <div className="sim-panel">
                {simulationQuery.error && (
                    <div className="citycore-error-banner" role="alert">
                        <span>{simulationQuery.error}</span>
                        <Button size="sm" onClick={() => void simulationQuery.refetch()}>
                            Retry
                        </Button>
                    </div>
                )}

                {simulationMutations.error && (
                    <div className="citycore-error-banner" role="alert">
                        <span>{simulationMutations.error}</span>
                        <Button size="sm" onClick={() => simulationMutations.clearError()}>
                            Dismiss
                        </Button>
                    </div>
                )}

                <ClockDisplay
                    value={localDate}
                    tickId={clock?.tickId}
                    speed={clock?.speed}
                    state={clock?.state}
                />

                <div className="sim-panel__section">
                    <h3 className="sim-panel__section-title">Transport</h3>

                    <div className="sim-controls-row">
                        <Button
                            onClick={() => void runCommand(() => simulationMutations.pause(cityId))}
                            disabled={!clock || simulationMutations.isSubmitting || !isRunning}
                        >
                            Pause
                        </Button>

                        <Button
                            variant="success"
                            onClick={() => void runCommand(() => simulationMutations.resume(cityId))}
                            disabled={!clock || simulationMutations.isSubmitting || isRunning}
                        >
                            Resume
                        </Button>
                    </div>
                </div>

                <div className="sim-panel__section">
                    <h3 className="sim-panel__section-title">Speed presets</h3>

                    <div className="sim-controls-row">
                        {[0.5, 1, 2, 5, 10].map((value) => (
                            <Button
                                key={value}
                                size="sm"
                                onClick={() => void runCommand(() => simulationMutations.setSpeed(cityId, value))}
                                disabled={!clock || simulationMutations.isSubmitting}
                            >
                                {value}x
                            </Button>
                        ))}
                    </div>
                </div>

                <div className="sim-panel__section">
                    <h3 className="sim-panel__section-title">Custom speed</h3>

                    <form className="sim-inline-form" onSubmit={onSetCustomSpeed}>
                        <input
                            className="text-input"
                            type="number"
                            min="0.01"
                            step="0.01"
                            value={customSpeed}
                            onChange={(event) => {
                                setCustomSpeed(event.target.value);
                                setCustomSpeedError(null);
                                simulationMutations.clearError();
                            }}
                        />
                        <Button
                            type="submit"
                            variant="primary"
                            disabled={simulationMutations.isSubmitting || !clock}
                        >
                            Set custom speed
                        </Button>
                    </form>

                    {customSpeedError && (
                        <div className="city-inline-error" role="alert">
                            {customSpeedError}
                        </div>
                    )}
                </div>

                <div className="sim-panel__section">
                    <h3 className="sim-panel__section-title">Jump time</h3>

                    <form className="sim-inline-form" onSubmit={onJump}>
                        <input
                            className="text-input"
                            type="datetime-local"
                            step="1"
                            value={jumpInput}
                            onChange={(event) => {
                                setJumpInput(event.target.value);
                                setJumpError(null);
                                simulationMutations.clearError();
                            }}
                        />
                        <Button
                            type="submit"
                            variant="primary"
                            disabled={simulationMutations.isSubmitting || !clock}
                        >
                            Jump time
                        </Button>
                    </form>

                    {jumpError && (
                        <div className="city-inline-error" role="alert">
                            {jumpError}
                        </div>
                    )}

                    <div className="city-details-hint">
                        Jump time uses local browser date/time input and converts it to UTC before sending to backend.
                    </div>
                </div>
            </div>
        </Card>
    );
};

export default SimulationPanel;
