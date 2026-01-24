import "@services/citycore/simulation/styles/clock-display.css";

interface ClockDisplayProps {
    value: Date | null;
    tickId?: number;
    speed?: number;
    state?: string;
}

function pad2(value: number): string {
    return String(value).padStart(2, "0");
}

function formatClockTimeUtc(value: Date | null): string {
    if (!value || Number.isNaN(value.getTime())) {
        return "--:--:--";
    }

    return [
        pad2(value.getUTCHours()),
        pad2(value.getUTCMinutes()),
        pad2(value.getUTCSeconds()),
    ].join(":");
}

function formatClockDateUtc(value: Date | null): string {
    if (!value || Number.isNaN(value.getTime())) {
        return "---- -- --";
    }

    return [
        value.getUTCFullYear(),
        pad2(value.getUTCMonth() + 1),
        pad2(value.getUTCDate()),
    ].join("-");
}

function getStateClassName(state?: string): string {
    const normalized = state?.toLowerCase();

    if (normalized === "running") {
        return "sim-clock-display__state sim-clock-display__state--running";
    }

    if (normalized === "paused") {
        return "sim-clock-display__state sim-clock-display__state--paused";
    }

    return "sim-clock-display__state";
}

const ClockDisplay = ({ value, tickId, speed, state }: ClockDisplayProps) => {
    return (
        <div className="sim-clock-display">
            <div className="sim-clock-display__screen">
                <div className="sim-clock-display__label">Simulation time</div>

                <div className="sim-clock-display__time">
                    {formatClockTimeUtc(value)}
                </div>

                <div className="sim-clock-display__subline">
          <span className="sim-clock-display__date">
            {formatClockDateUtc(value)}
          </span>
                    <span className="sim-clock-display__timezone">UTC</span>
                </div>
            </div>

            <div className="sim-clock-display__meta">
                <span>Tick: {tickId ?? "--"}</span>
                <span>Speed: {speed ?? "--"}x</span>
                <span className={getStateClassName(state)}>
          State: {state ?? "--"}
        </span>
            </div>
        </div>
    );
};

export default ClockDisplay;
