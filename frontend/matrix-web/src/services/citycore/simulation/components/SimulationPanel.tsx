import {type FormEvent, useEffect, useMemo, useRef, useState} from "react";
import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import ClockDisplay from "@services/citycore/simulation/components/ClockDisplay";
import {useSimulationClock} from "@services/citycore/simulation/hooks/useSimulationClock";
import {useSimulationMutations} from "@services/citycore/simulation/hooks/useSimulationMutations";
import {localDateTimeToUtcIso} from "@services/citycore/scenarios/classic-city/utils/dateTime";
import "@services/citycore/simulation/styles/simulation-panel.css";

const TICK_INTERVAL_MS = 250;

interface SimulationPanelProps {
    simulationId: string;
    isReadOnly?: boolean;
    readOnlyMessage?: string;
}

interface LocalClockBase {
    simMs: number;
    syncedAtMs: number;
    speed: number;
    isRunning: boolean;
}

function getMonotonicNow(): number {
    return performance.now();
}

function getClockSpeed(speed: number | undefined): number {
    return typeof speed === "number" && Number.isFinite(speed) ? speed : 1;
}

function readClockValue(base: LocalClockBase, nowMs: number): number {
    if (!base.isRunning) {
        return base.simMs;
    }

    return base.simMs + Math.max(0, nowMs - base.syncedAtMs) * base.speed;
}

const SimulationPanel = ({
    simulationId,
    isReadOnly = false,
    readOnlyMessage,
}: SimulationPanelProps) => {
    const simulationQuery = useSimulationClock(simulationId, isReadOnly ? 0 : 5000);
    const simulationMutations = useSimulationMutations();

    const [localSimMs, setLocalSimMs] = useState<number | null>(null);
    const [customSpeed, setCustomSpeed] = useState("1");
    const [jumpInput, setJumpInput] = useState("");
    const [customSpeedError, setCustomSpeedError] = useState<string | null>(null);
    const [jumpError, setJumpError] = useState<string | null>(null);

    const localClockBaseRef = useRef<LocalClockBase | null>(null);

    const clock = simulationQuery.data;
    const isRunning = !isReadOnly && clock?.state.toLowerCase() === "running";
    const displayState = isReadOnly ? "Paused" : clock?.state;
    const modeLabel = isReadOnly ? "Archived snapshot" : isRunning ? "Live sync" : "Paused snapshot";

    useEffect(() => {
        if (!clock) {
            setLocalSimMs(null);
            localClockBaseRef.current = null;
            return;
        }

        const serverMs = new Date(clock.simTimeUtc).getTime();

        if (!Number.isFinite(serverMs)) {
            return;
        }

        const receivedAtMs = simulationQuery.syncMeta?.receivedAtMs ?? getMonotonicNow();
        const requestedAtMs = simulationQuery.syncMeta?.requestedAtMs ?? receivedAtMs;
        const roundTripMs = Math.max(0, receivedAtMs - requestedAtMs);
        const speed = getClockSpeed(clock.speed);
        const nextBase: LocalClockBase = {
            simMs: serverMs + (isRunning ? roundTripMs * 0.5 * speed : 0),
            syncedAtMs: receivedAtMs,
            speed,
            isRunning,
        };

        localClockBaseRef.current = nextBase;
        setLocalSimMs(readClockValue(nextBase, receivedAtMs));
    }, [clock, isRunning, simulationQuery.syncMeta]);

    useEffect(() => {
        if (!isReadOnly) {
            return;
        }

        setLocalSimMs((current) => {
            if (current === null) {
                return current;
            }

            const nowMs = getMonotonicNow();
            localClockBaseRef.current = {
                simMs: current,
                syncedAtMs: nowMs,
                speed: getClockSpeed(clock?.speed),
                isRunning: false,
            };
            return current;
        });
    }, [clock?.speed, isReadOnly]);

    useEffect(() => {
        const tick = () => {
            const base = localClockBaseRef.current;

            if (!base) {
                setLocalSimMs((current) => (current === null ? current : null));
                return;
            }

            const nextValue = readClockValue(base, getMonotonicNow());
            setLocalSimMs((current) => {
                if (current !== null && Math.abs(current - nextValue) < 10) {
                    return current;
                }

                return nextValue;
            });
        };

        tick();

        const timerId = window.setInterval(tick, TICK_INTERVAL_MS);

        return () => {
            window.clearInterval(timerId);
        };
    }, []);

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
        await runCommand(() => simulationMutations.setSpeed(simulationId, speed));
    };

    const onJump = async (event: FormEvent) => {
        event.preventDefault();

        const utcIso = localDateTimeToUtcIso(jumpInput);

        if (!utcIso) {
            setJumpError("Please enter a valid date and time.");
            return;
        }

        setJumpError(null);
        await runCommand(() => simulationMutations.jump(simulationId, utcIso));
    };

    return (
        <Card
            title="Simulation"
            subtitle={isReadOnly ? "Clock snapshot for an archived city" : "Clock state and controls"}
            right={<span
                className={`sim-panel__mode-badge ${isReadOnly ? "sim-panel__mode-badge--archived" : ""}`}>{modeLabel}</span>}
            className={isReadOnly ? "sim-panel-card sim-panel-card--readonly" : "sim-panel-card"}
        >
            <div className="sim-panel">
                {simulationQuery.error ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{simulationQuery.error}</span>
                        <Button size="sm" onClick={() => void simulationQuery.refetch()}>
                            Retry
                        </Button>
                    </div>
                ) : null}

                {simulationMutations.error ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{simulationMutations.error}</span>
                        <Button size="sm" onClick={() => simulationMutations.clearError()}>
                            Dismiss
                        </Button>
                    </div>
                ) : null}

                {isReadOnly ? (
                    <div className="sim-panel__notice" role="status">
                        <div className="sim-panel__notice-title">Archived city</div>
                        <div className="sim-panel__notice-text">
                            {readOnlyMessage ?? "Simulation mutations are disabled for archived cities."}
                        </div>
                    </div>
                ) : null}

                <div className="sim-panel__snapshot-grid">
                    <div className="sim-panel__snapshot-item">
                        <span className="sim-panel__snapshot-label">Mode</span>
                        <strong className="sim-panel__snapshot-value">{modeLabel}</strong>
                    </div>
                    <div className="sim-panel__snapshot-item">
                        <span className="sim-panel__snapshot-label">Refresh cadence</span>
                        <strong className="sim-panel__snapshot-value">{isReadOnly ? "Manual" : "Every 5 seconds"}</strong>
                    </div>
                </div>

                <ClockDisplay
                    value={localDate}
                    tickId={clock?.tickId}
                    speed={clock?.speed}
                    state={displayState}
                />

                {!isReadOnly ? (
                    <>
                        <div className="sim-panel__section">
                            <h3 className="sim-panel__section-title">Transport</h3>

                            <div className="sim-controls-row">
                                <Button
                                    onClick={() => void runCommand(() => simulationMutations.pause(simulationId))}
                                    disabled={!clock || simulationMutations.isSubmitting || !isRunning}
                                >
                                    Pause
                                </Button>

                                <Button
                                    variant="success"
                                    onClick={() => void runCommand(() => simulationMutations.resume(simulationId))}
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
                                        onClick={() => void runCommand(() => simulationMutations.setSpeed(simulationId, value))}
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

                            {customSpeedError ? (
                                <div className="city-inline-error" role="alert">
                                    {customSpeedError}
                                </div>
                            ) : null}
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

                            {jumpError ? (
                                <div className="city-inline-error" role="alert">
                                    {jumpError}
                                </div>
                            ) : null}

                            <div className="city-details-hint">
                                Jump time uses local browser date/time input and converts it to UTC before sending to
                                backend.
                            </div>
                        </div>
                    </>
                ) : null}
            </div>
        </Card>
    );
};

export default SimulationPanel;
