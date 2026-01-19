import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import type { ClockState } from "@services/citycore/api/cityCoreTypes";

interface SimulationControlsProps {
    state: ClockState | null;
    hasClock: boolean;
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
                    disabled={isBootstrapping || hasClock}
                >
                    {isBootstrapping ? "Bootstrapping..." : "Bootstrap"}
                </Button>

                <Button
                    onClick={() => void onPause()}
                    disabled={!hasClock || isPaused || isPausing}
                >
                    {isPausing ? "Pausing..." : "Pause"}
                </Button>

                <Button
                    variant="success"
                    onClick={() => void onResume()}
                    disabled={!hasClock || isRunning || isResuming}
                >
                    {isResuming ? "Resuming..." : "Resume"}
                </Button>
            </div>
        </Card>
    );
};

export default SimulationControls;
