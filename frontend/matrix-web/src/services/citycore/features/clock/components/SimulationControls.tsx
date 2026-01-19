import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import type { ClockState } from "@services/citycore/api/cityCoreTypes";

interface SimulationControlsProps {
    state: ClockState | null;
    hasClock: boolean;
    canBootstrap: boolean;
    canControlClock: boolean;
    isBootstrapping: boolean;
    isPausing: boolean;
    isResuming: boolean;
    onBootstrap: () => Promise<void>;
    onPause: () => Promise<void>;
    onResume: () => Promise<void>;
}

const SimulationControls = ({
                                state,
                                hasClock,
                                canBootstrap,
                                canControlClock,
                                isBootstrapping,
                                isPausing,
                                isResuming,
                                onBootstrap,
                                onPause,
                                onResume,
                            }: SimulationControlsProps) => {
    const isPaused = state === "Paused";
    const isRunning = state === "Running";

    return (
        <Card title="Simulation controls" subtitle="Clock lifecycle operations">
            <div className="actions-row">
                <Button
                    variant="primary"
                    onClick={() => void onBootstrap()}
                    disabled={!canBootstrap || isBootstrapping}
                >
                    {isBootstrapping ? "Bootstrapping..." : "Bootstrap"}
                </Button>

                <Button
                    onClick={() => void onPause()}
                    disabled={!hasClock || !canControlClock || isPaused || isPausing}
                >
                    {isPausing ? "Pausing..." : "Pause"}
                </Button>

                <Button
                    variant="success"
                    onClick={() => void onResume()}
                    disabled={!hasClock || !canControlClock || isRunning || isResuming}
                >
                    {isResuming ? "Resuming..." : "Resume"}
                </Button>
            </div>
        </Card>
    );
};

export default SimulationControls;
