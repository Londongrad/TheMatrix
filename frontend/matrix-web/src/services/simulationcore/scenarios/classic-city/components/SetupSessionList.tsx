import Button from "@shared/ui/controls/Button/Button";
import type {ClassicCitySetupSessionView} from "@services/simulationcore/scenarios/classic-city/contracts/setupSessionContracts";

interface SetupSessionListProps {
    sessions: ClassicCitySetupSessionView[];
    onOpen: (session: ClassicCitySetupSessionView) => void;
}

function formatSetupSessionStatus(status: string): string {
    return status === "LaunchFailed"
        ? "Launch failed"
        : "Draft";
}

function getSetupSessionTone(status: string): "draft" | "failed" {
    return status === "LaunchFailed"
        ? "failed"
        : "draft";
}

function formatSetupStep(stepId: string): string {
    switch (stepId) {
        case "profile":
            return "Profile";
        case "environment":
            return "Environment";
        case "population":
            return "Population";
        case "launch":
            return "Launch";
        case "scenario":
        default:
            return "Scenario";
    }
}

function formatTimestamp(value?: string | null): string {
    if (!value) {
        return "Unknown";
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime())
        ? "Unknown"
        : parsed.toLocaleString();
}

function getDraftName(session: ClassicCitySetupSessionView): string {
    return session.draft.name.trim().length > 0
        ? session.draft.name.trim()
        : "Untitled Classic City";
}

export default function SetupSessionList({sessions, onOpen}: SetupSessionListProps) {
    return (
        <div className="city-list-grid">
            {sessions.map((session) => {
                const tone = getSetupSessionTone(session.status);
                const statusLabel = formatSetupSessionStatus(session.status);

                return (
                    <article key={session.sessionId} className={`setup-session-card setup-session-card--${tone}`}>
                        <div className="setup-session-card__topline">
                            <span className={`cities-status-pill cities-status-pill--${tone}`}>
                                {statusLabel}
                            </span>

                            <span className="setup-session-card__id" title={session.sessionId}>
                                {session.sessionId.slice(0, 8)}
                            </span>
                        </div>

                        <div className="setup-session-card__body">
                            <h3 className="setup-session-card__name">{getDraftName(session)}</h3>
                            <p className="setup-session-card__description">
                                Current step: {formatSetupStep(session.currentStepId)}
                            </p>
                            <p className="setup-session-card__description">
                                Last saved: {formatTimestamp(session.updatedAtUtc)}
                            </p>
                            <p className={`setup-session-card__description${tone === "failed" ? " setup-session-card__description--failed" : ""}`}>
                                {session.failureMessage?.trim() || "Resume the setup wizard from the last backend-saved draft state."}
                            </p>
                        </div>

                        <div className="setup-session-card__footer">
                            <div className="setup-session-card__footer-copy">
                                <div className="setup-session-card__footer-label">Draft session</div>
                                <div className="setup-session-card__footer-value">{statusLabel}</div>
                            </div>

                            <Button
                                size="sm"
                                variant={tone === "failed" ? "default" : "primary"}
                                onClick={() => onOpen(session)}
                            >
                                {tone === "failed" ? "Fix and resume" : "Resume draft"}
                            </Button>
                        </div>
                    </article>
                );
            })}
        </div>
    );
}
