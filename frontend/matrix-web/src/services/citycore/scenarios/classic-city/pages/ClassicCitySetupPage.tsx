import {useEffect, useRef, useState} from "react";
import {Link, useNavigate, useParams} from "react-router-dom";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Button from "@shared/ui/controls/Button/Button";
import {
    createClassicCitySetupSession,
    getClassicCitySetupSession,
    launchClassicCitySetupSession,
    updateClassicCitySetupSession,
} from "@services/citycore/scenarios/classic-city/api/setupSessionsApi";
import type {
    ClassicCityPopulationMode,
    ClassicCitySetupDraftView,
    ClassicCitySetupSessionView,
    ClassicCitySetupStepId,
} from "@services/citycore/scenarios/classic-city/contracts/setupSessionContracts";
import {
    CLASSIC_CITY_CLIMATE_OPTIONS,
    CLASSIC_CITY_DEVELOPMENT_OPTIONS,
    CLASSIC_CITY_DENSITY_OPTIONS,
    CLASSIC_CITY_HEMISPHERE_OPTIONS,
    CLASSIC_CITY_POPULATION_MODE_OPTIONS,
    CLASSIC_CITY_SIZE_TIER_OPTIONS,
    type SetupOption,
} from "@services/citycore/scenarios/classic-city/setupOptions";
import {
    getNowLocalDateTimeInputValue,
    localDateTimeToUtcIso,
} from "@services/citycore/simulation/utils/dateTime";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_SCENARIO,
    getClassicCitySetupProvisioningPath,
    getClassicCitySetupSessionPath,
} from "@services/citycore/scenarios/registry";
import "@services/citycore/scenarios/styles/scenario-setup.css";

type ValidationErrors = {
    name?: string;
    startSimTimeLocal?: string;
    speedMultiplier?: string;
    utcOffsetMinutes?: string;
    plannedPeopleCount?: string;
};

type SetupDraft = ClassicCitySetupDraftView;

type SessionSnapshot = {
    currentStepId: ClassicCitySetupStepId;
    draft: SetupDraft;
};

type OptionGridProps = {
    legend: string;
    options: SetupOption[];
    selectedValue: string;
    onSelect: (value: string) => void;
    disabled?: boolean;
};

const setupSteps: { id: ClassicCitySetupStepId; title: string; description: string }[] = [
    {
        id: "scenario",
        title: "Scenario",
        description: "Choose the simulation baseline and what the launch flow will provision.",
    },
    {
        id: "profile",
        title: "City profile",
        description: "Define the city identity, timeline, and generation profile.",
    },
    {
        id: "environment",
        title: "Environment",
        description: "Set the climate context that will drive weather planning and downstream bootstrap.",
    },
    {
        id: "population",
        title: "Population",
        description: "Choose whether bootstrap should derive population automatically or launch against a fixed resident target.",
    },
    {
        id: "launch",
        title: "Launch review",
        description: "Verify the launch contract before handing the setup off to backend provisioning.",
    },
];

const mutableSessionStatuses = new Set(["Draft", "LaunchFailed"]);
const runningSessionStatuses = new Set(["LaunchQueued", "CreatingCity", "BootstrappingPopulation"]);
const MAX_PLANNED_PEOPLE_COUNT = 1_000_000;

function createDefaultDraft(): SetupDraft {
    const startSimTimeLocal = getNowLocalDateTimeInputValue();

    return {
        name: "",
        startSimTimeLocal,
        startSimTimeUtc: localDateTimeToUtcIso(startSimTimeLocal),
        speedMultiplier: "1",
        climateZone: "Temperate",
        hemisphere: "Northern",
        utcOffsetMinutes: String(-new Date().getTimezoneOffset()),
        generationSeed: "",
        sizeTier: "Medium",
        urbanDensity: "Balanced",
        developmentLevel: "Balanced",
        populationMode: "automatic",
        plannedPeopleCount: "",
    };
}

function normalizeDraft(draft: SetupDraft): SetupDraft {
    return {
        ...draft,
        startSimTimeUtc: draft.startSimTimeUtc ?? localDateTimeToUtcIso(draft.startSimTimeLocal),
        populationMode: draft.populationMode === "manual" ? "manual" : "automatic",
        plannedPeopleCount: draft.plannedPeopleCount?.trim() ?? "",
    };
}

function createSnapshotSignature(snapshot: SessionSnapshot): string {
    return JSON.stringify({
        currentStepId: snapshot.currentStepId,
        draft: normalizeDraft(snapshot.draft),
    });
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

function validateProfile(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};

    if (!draft.name.trim()) {
        errors.name = "City name is required.";
    }

    if (!draft.startSimTimeLocal.trim()) {
        errors.startSimTimeLocal = "Start simulation time is required.";
    } else if (!localDateTimeToUtcIso(draft.startSimTimeLocal)) {
        errors.startSimTimeLocal = "Invalid date/time value.";
    }

    const speed = Number(draft.speedMultiplier);
    if (!draft.speedMultiplier.trim()) {
        errors.speedMultiplier = "Speed multiplier is required.";
    } else if (!Number.isFinite(speed)) {
        errors.speedMultiplier = "Speed multiplier must be a number.";
    } else if (speed <= 0) {
        errors.speedMultiplier = "Speed multiplier must be greater than 0.";
    }

    return errors;
}

function validateEnvironment(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};
    const utcOffsetMinutes = Number(draft.utcOffsetMinutes);

    if (!draft.utcOffsetMinutes.trim()) {
        errors.utcOffsetMinutes = "UTC offset is required.";
    } else if (!Number.isInteger(utcOffsetMinutes)) {
        errors.utcOffsetMinutes = "UTC offset must be a whole number of minutes.";
    } else if (utcOffsetMinutes < -14 * 60 || utcOffsetMinutes > 14 * 60) {
        errors.utcOffsetMinutes = "UTC offset must stay between -840 and 840 minutes.";
    }

    return errors;
}

function validatePopulation(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};

    if (draft.populationMode !== "manual") {
        return errors;
    }

    const plannedPeopleCount = Number(draft.plannedPeopleCount);

    if (!draft.plannedPeopleCount.trim()) {
        errors.plannedPeopleCount = "Planned people count is required for manual bootstrap.";
    } else if (!Number.isInteger(plannedPeopleCount)) {
        errors.plannedPeopleCount = "Planned people count must be a whole number.";
    } else if (plannedPeopleCount < 0) {
        errors.plannedPeopleCount = "Planned people count cannot be negative.";
    } else if (plannedPeopleCount > MAX_PLANNED_PEOPLE_COUNT) {
        errors.plannedPeopleCount = `Planned people count must stay below ${MAX_PLANNED_PEOPLE_COUNT.toLocaleString()}.`;
    }

    return errors;
}

function mergeErrors(...items: ValidationErrors[]): ValidationErrors {
    return Object.assign({}, ...items);
}

function getStepIndex(stepId: ClassicCitySetupStepId): number {
    return Math.max(0, setupSteps.findIndex((step) => step.id === stepId));
}

function formatUtcOffset(minutesText: string): string {
    const minutes = Number(minutesText);
    if (!Number.isFinite(minutes)) {
        return "--";
    }

    const sign = minutes >= 0 ? "+" : "-";
    const absoluteMinutes = Math.abs(minutes);
    const hours = Math.floor(absoluteMinutes / 60)
        .toString()
        .padStart(2, "0");
    const mins = (absoluteMinutes % 60)
        .toString()
        .padStart(2, "0");

    return `UTC ${sign}${hours}:${mins}`;
}

function formatDateTime(value?: string | null): string {
    if (!value) {
        return "--";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

function formatPeopleCount(value: string): string {
    const parsed = Number(value);
    if (!Number.isFinite(parsed)) {
        return value;
    }

    return parsed.toLocaleString();
}

function getPopulationPlanLabel(mode: ClassicCityPopulationMode): string {
    return mode === "manual" ? "Manual target" : "Automatic bootstrap";
}

function getPopulationPlanDescription(draft: SetupDraft): string {
    if (draft.populationMode === "manual") {
        return draft.plannedPeopleCount.trim().length > 0
            ? `${formatPeopleCount(draft.plannedPeopleCount)} residents requested before provisioning starts.`
            : "Manual bootstrap requires an explicit resident target.";
    }

    return "Gateway derives the opening headcount from the generated residential capacity and city profile.";
}

function formatSessionStatusLabel(status?: string | null): string {
    switch (status) {
        case "Draft":
            return "Draft";
        case "LaunchQueued":
            return "Launch queued";
        case "CreatingCity":
            return "Creating city";
        case "BootstrappingPopulation":
            return "Bootstrapping population";
        case "Ready":
            return "Ready";
        case "ProvisioningFailed":
            return "Provisioning failed";
        case "LaunchFailed":
            return "Launch failed";
        default:
            return "Preparing session";
    }
}

function getSessionStatusTone(status?: string | null): "draft" | "running" | "ready" | "failed" {
    if (status === "Ready") {
        return "ready";
    }

    if (status === "LaunchFailed" || status === "ProvisioningFailed") {
        return "failed";
    }

    if (status && runningSessionStatuses.has(status)) {
        return "running";
    }

    return "draft";
}

function isMutableSessionStatus(status?: string | null): boolean {
    return status ? mutableSessionStatuses.has(status) : false;
}

function isRunningSessionStatus(status?: string | null): boolean {
    return status ? runningSessionStatuses.has(status) : false;
}

function getSessionStatusDescription(session: ClassicCitySetupSessionView | null): string {
    switch (session?.status) {
        case "LaunchQueued":
            return "The launch request is queued in Gateway and will survive page refresh or tab closure.";
        case "CreatingCity":
            return "CityCore is creating topology, clock, and initial environment for the requested launch contract.";
        case "BootstrappingPopulation":
            return "Population bootstrap is running downstream. The city will hand off to provisioning as soon as a host id is available.";
        case "LaunchFailed":
            return session.failureMessage ?? "Launch orchestration failed before the city host was created.";
        case "ProvisioningFailed":
            return session.failureMessage ?? "Population bootstrap failed after city creation and requires operator review.";
        case "Ready":
            return "The setup session completed successfully and is ready to hand off to monitoring.";
        default:
            return "Draft changes are saved to a backend setup session so the wizard can be resumed after refresh.";
    }
}

function OptionGrid({legend, options, selectedValue, onSelect, disabled = false}: OptionGridProps) {
    return (
        <div className="scenario-setup__field">
            <div className="scenario-setup__label">{legend}</div>
            <div className="scenario-setup__option-grid">
                {options.map((option) => {
                    const isSelected = option.value === selectedValue;

                    return (
                        <button
                            key={option.value}
                            type="button"
                            className={`scenario-setup__option-card ${isSelected ? "scenario-setup__option-card--selected" : ""}`}
                            onClick={() => onSelect(option.value)}
                            disabled={disabled}
                        >
                            <span className="scenario-setup__option-title">{option.label}</span>
                            <span className="scenario-setup__option-text">{option.description}</span>
                        </button>
                    );
                })}
            </div>
        </div>
    );
}

export default function ClassicCitySetupPage() {
    const params = useParams<{ sessionId?: string }>();
    const routeSessionId = params.sessionId ?? null;
    const navigate = useNavigate();
    const [session, setSession] = useState<ClassicCitySetupSessionView | null>(null);
    const [draft, setDraft] = useState<SetupDraft>(createDefaultDraft);
    const [currentStepId, setCurrentStepId] = useState<ClassicCitySetupStepId>("scenario");
    const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
    const [isInitializing, setIsInitializing] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [isLaunching, setIsLaunching] = useState(false);
    const [pageError, setPageError] = useState<string | null>(null);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [lastSavedAtUtc, setLastSavedAtUtc] = useState<string | null>(null);
    const saveTimeoutRef = useRef<number | null>(null);
    const saveAbortRef = useRef<AbortController | null>(null);
    const lastSyncedSignatureRef = useRef<string | null>(null);
    const latestSnapshotRef = useRef<SessionSnapshot>({
        currentStepId: "scenario",
        draft: createDefaultDraft(),
    });

    latestSnapshotRef.current = {
        currentStepId,
        draft,
    };

    const currentStepIndex = getStepIndex(currentStepId);
    const currentStep = setupSteps[currentStepIndex];
    const sessionStatusLabel = formatSessionStatusLabel(session?.status);
    const sessionStatusTone = getSessionStatusTone(session?.status);
    const canEditSession = isMutableSessionStatus(session?.status);
    const isLaunchRunning = isRunningSessionStatus(session?.status);
    const isBusy = isInitializing || isLaunching;

    function adoptSession(nextSession: ClassicCitySetupSessionView, syncLocalState = true) {
        const normalizedDraft = normalizeDraft(nextSession.draft);

        setSession(nextSession);
        setLastSavedAtUtc(nextSession.updatedAtUtc);
        lastSyncedSignatureRef.current = createSnapshotSignature({
            currentStepId: nextSession.currentStepId,
            draft: normalizedDraft,
        });

        if (syncLocalState) {
            setDraft(normalizedDraft);
            setCurrentStepId(nextSession.currentStepId);
        }
    }

    function clearPendingAutosave() {
        if (saveTimeoutRef.current) {
            window.clearTimeout(saveTimeoutRef.current);
            saveTimeoutRef.current = null;
        }
    }

    async function persistDraftNow(force = false): Promise<ClassicCitySetupSessionView | null> {
        if (!session?.sessionId || !canEditSession) {
            return session;
        }

        const snapshot = latestSnapshotRef.current;
        const signature = createSnapshotSignature(snapshot);

        if (!force && signature === lastSyncedSignatureRef.current) {
            return session;
        }

        clearPendingAutosave();
        saveAbortRef.current?.abort();

        const abortController = new AbortController();
        saveAbortRef.current = abortController;

        try {
            setIsSaving(true);
            setSaveError(null);

            const updatedSession = await updateClassicCitySetupSession(
                session.sessionId,
                {
                    currentStepId: snapshot.currentStepId,
                    draft: snapshot.draft,
                },
                abortController.signal,
            );

            if (abortController.signal.aborted) {
                return null;
            }

            const normalizedDraft = normalizeDraft(updatedSession.draft);
            const liveSignature = createSnapshotSignature(latestSnapshotRef.current);

            setSession(updatedSession);
            setLastSavedAtUtc(updatedSession.updatedAtUtc);
            lastSyncedSignatureRef.current = createSnapshotSignature({
                currentStepId: updatedSession.currentStepId,
                draft: normalizedDraft,
            });

            if (liveSignature === signature) {
                setDraft(normalizedDraft);
                setCurrentStepId(updatedSession.currentStepId);
            }

            return updatedSession;
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return null;
            }

            setSaveError(getErrorMessage(error, "Failed to save setup session."));
            return null;
        } finally {
            if (!abortController.signal.aborted) {
                setIsSaving(false);
            }

            if (saveAbortRef.current === abortController) {
                saveAbortRef.current = null;
            }
        }
    }

    function updateDraft<K extends keyof SetupDraft>(key: K, value: SetupDraft[K]) {
        setDraft((current) => {
            const next = {
                ...current,
                [key]: value,
            };

            if (key === "startSimTimeLocal") {
                next.startSimTimeUtc = localDateTimeToUtcIso(String(value));
            }

            return next;
        });

        setValidationErrors((current) => {
            if (key === "populationMode" && value === "automatic") {
                if (!current.plannedPeopleCount) {
                    return current;
                }

                const next = {...current};
                delete next.plannedPeopleCount;
                return next;
            }

            if (!(key in current)) {
                return current;
            }

            const next = {...current};
            delete next[key as keyof ValidationErrors];
            return next;
        });
        setPageError(null);
        setSaveError(null);
    }

    function moveToStep(stepId: ClassicCitySetupStepId) {
        setCurrentStepId(stepId);
        setPageError(null);
        setSaveError(null);
    }

    function goNext() {
        if (currentStep.id === "profile") {
            const errors = validateProfile(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        if (currentStep.id === "environment") {
            const errors = validateEnvironment(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        if (currentStep.id === "population") {
            const errors = validatePopulation(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        moveToStep(setupSteps[Math.min(currentStepIndex + 1, setupSteps.length - 1)].id);
    }

    function goBack() {
        moveToStep(setupSteps[Math.max(currentStepIndex - 1, 0)].id);
    }

    async function handleLaunch() {
        const errors = mergeErrors(
            validateProfile(draft),
            validateEnvironment(draft),
            validatePopulation(draft),
        );
        setValidationErrors(errors);

        if (Object.keys(errors).length > 0) {
            return;
        }

        if (!session?.sessionId) {
            setPageError("Setup session is still being prepared. Please try again.");
            return;
        }

        setPageError(null);
        setSaveError(null);
        setIsLaunching(true);

        try {
            const persisted = await persistDraftNow(true);
            if (!persisted) {
                return;
            }

            const launchedSession = await launchClassicCitySetupSession(session.sessionId);
            adoptSession(launchedSession);
            navigate(getClassicCitySetupProvisioningPath(launchedSession.sessionId), {
                replace: true,
                state: {
                    session: launchedSession,
                },
            });
        } catch (error: unknown) {
            setPageError(getErrorMessage(error, "Failed to queue Classic City launch."));
        } finally {
            setIsLaunching(false);
        }
    }

    useEffect(() => {
        let isDisposed = false;
        const abortController = new AbortController();

        async function initialize() {
            if (routeSessionId && session?.sessionId === routeSessionId) {
                setIsInitializing(false);
                return;
            }

            setIsInitializing(true);
            setPageError(null);

            try {
                if (routeSessionId) {
                    const loadedSession = await getClassicCitySetupSession(routeSessionId, abortController.signal);
                    if (isDisposed || abortController.signal.aborted) {
                        return;
                    }

                    adoptSession(loadedSession);
                    return;
                }

                const initialDraft = createDefaultDraft();
                const createdSession = await createClassicCitySetupSession({
                    currentStepId: "scenario",
                    draft: initialDraft,
                });

                if (isDisposed) {
                    return;
                }

                adoptSession(createdSession);
                navigate(getClassicCitySetupSessionPath(createdSession.sessionId), {replace: true});
            } catch (error: unknown) {
                if (abortController.signal.aborted || isDisposed) {
                    return;
                }

                setPageError(getErrorMessage(error, "Failed to prepare Classic City setup session."));
            } finally {
                if (!isDisposed) {
                    setIsInitializing(false);
                }
            }
        }

        void initialize();

        return () => {
            isDisposed = true;
            abortController.abort();
        };
    }, [navigate, routeSessionId, session?.sessionId]);

    useEffect(() => {
        if (!session?.sessionId || !canEditSession || isInitializing || isLaunching) {
            return;
        }

        const signature = createSnapshotSignature(latestSnapshotRef.current);
        if (signature === lastSyncedSignatureRef.current) {
            return;
        }

        clearPendingAutosave();
        saveTimeoutRef.current = window.setTimeout(() => {
            void persistDraftNow();
        }, 700);

        return () => {
            clearPendingAutosave();
        };
    }, [canEditSession, currentStepId, draft, isInitializing, isLaunching, session?.sessionId]);

    useEffect(() => {
        if (!session?.sessionId || !isLaunchRunning) {
            return;
        }

        const timer = window.setTimeout(async () => {
            try {
                const refreshedSession = await getClassicCitySetupSession(session.sessionId);
                adoptSession(refreshedSession, true);
            } catch (error: unknown) {
                setPageError(getErrorMessage(error, "Failed to refresh setup session status."));
            }
        }, 2500);

        return () => {
            window.clearTimeout(timer);
        };
    }, [isLaunchRunning, session?.sessionId, session?.status]);

    useEffect(() => {
        if (!session?.sessionId || canEditSession) {
            return;
        }

        navigate(getClassicCitySetupProvisioningPath(session.sessionId), {replace: true});
    }, [canEditSession, navigate, session?.sessionId]);

    useEffect(() => {
        return () => {
            clearPendingAutosave();
            saveAbortRef.current?.abort();
        };
    }, []);

    if (isInitializing && !session) {
        return (
            <section className="scenario-setup">
                <div className="scenario-setup__panel">
                    <LoadingIndicator label="Preparing Classic City setup session..."/>
                </div>
            </section>
        );
    }

    return (
        <section className="scenario-setup">
            <header className="scenario-setup__hero">
                <div className="scenario-setup__eyebrow">Compose scenario</div>
                <div className="scenario-setup__hero-grid">
                    <div className="scenario-setup__hero-copy">
                        <div className="scenario-setup__status-row">
                            <span className={`scenario-setup__status-chip scenario-setup__status-chip--${sessionStatusTone}`}>
                                {sessionStatusLabel}
                            </span>
                            {session?.sessionId ? (
                                <span className="scenario-setup__status-meta">
                                    Session {session.sessionId.slice(0, 8)}
                                </span>
                            ) : null}
                        </div>

                        <h1 className="scenario-setup__title">Classic City setup</h1>
                        <p className="scenario-setup__subtitle">
                            Build the launch contract in steps, keep the draft in a backend setup session, and hand
                            the city off to provisioning only after the orchestration flow has actually started.
                        </p>
                    </div>

                    <div className="scenario-setup__hero-art" aria-hidden="true">
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--one"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--two"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--three"/>
                    </div>
                </div>

                <div className="scenario-setup__stepper" aria-label="Classic City setup progress">
                    {setupSteps.map((step, index) => {
                        const isCurrent = index === currentStepIndex;
                        const isComplete = index < currentStepIndex;

                        return (
                            <div
                                key={step.id}
                                className={`scenario-setup__step ${isCurrent ? "scenario-setup__step--current" : ""} ${isComplete ? "scenario-setup__step--complete" : ""}`}
                            >
                                <span className="scenario-setup__step-index">{index + 1}</span>
                                <div className="scenario-setup__step-copy">
                                    <span className="scenario-setup__step-title">{step.title}</span>
                                    <span className="scenario-setup__step-text">{step.description}</span>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </header>

            <div className="scenario-setup__layout">
                <div className="scenario-setup__panel">
                    <div className="scenario-setup__panel-header">
                        <div>
                            <div className="scenario-setup__panel-eyebrow">{currentStep.title}</div>
                            <h2 className="scenario-setup__panel-title">{currentStep.description}</h2>
                        </div>

                        <Link className="scenario-setup__ghost-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                            Back to catalog
                        </Link>
                    </div>

                    {pageError ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {pageError}
                        </div>
                    ) : null}

                    {saveError ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {saveError}
                        </div>
                    ) : null}

                    {session?.failureMessage && session.status === "LaunchFailed" ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {session.failureMessage}
                        </div>
                    ) : null}

                    {currentStep.id === "scenario" ? (
                        <div className="scenario-setup__stack">
                            <article className="scenario-setup__scenario-card">
                                <div className="scenario-setup__scenario-topline">
                                    <span className="scenario-setup__scenario-chip">{CLASSIC_CITY_SCENARIO.kind}</span>
                                    <span className="scenario-setup__scenario-chip scenario-setup__scenario-chip--accent">
                                        {CLASSIC_CITY_SCENARIO.availabilityLabel}
                                    </span>
                                </div>

                                <h3 className="scenario-setup__scenario-title">{CLASSIC_CITY_SCENARIO.label}</h3>
                                <p className="scenario-setup__scenario-summary">{CLASSIC_CITY_SCENARIO.summary}</p>
                                <p className="scenario-setup__scenario-description">{CLASSIC_CITY_SCENARIO.description}</p>

                                <div className="scenario-setup__highlight-list">
                                    {CLASSIC_CITY_SCENARIO.highlights.map((item) => (
                                        <div key={item} className="scenario-setup__highlight-item">
                                            {item}
                                        </div>
                                    ))}
                                </div>
                            </article>

                            <div className="scenario-setup__note">
                                The launch contract now lives in a real setup session resource. You can refresh,
                                navigate away, and come back without losing the authoring state or launch lifecycle.
                            </div>
                        </div>
                    ) : null}

                    {currentStep.id === "profile" ? (
                        <div className="scenario-setup__stack">
                            <div className="scenario-setup__form-grid">
                                <div className="scenario-setup__field scenario-setup__field--wide">
                                    <label className="scenario-setup__label" htmlFor="classic-city-name">
                                        City name
                                    </label>
                                    <input
                                        id="classic-city-name"
                                        className="scenario-setup__input"
                                        value={draft.name}
                                        maxLength={128}
                                        placeholder="New Amsterdam"
                                        onChange={(event) => updateDraft("name", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.name ? (
                                        <div className="scenario-setup__error">{validationErrors.name}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-start-time">
                                        Start simulation time
                                    </label>
                                    <input
                                        id="classic-city-start-time"
                                        className="scenario-setup__input"
                                        type="datetime-local"
                                        value={draft.startSimTimeLocal}
                                        onChange={(event) => updateDraft("startSimTimeLocal", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.startSimTimeLocal ? (
                                        <div className="scenario-setup__error">{validationErrors.startSimTimeLocal}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-speed">
                                        Speed multiplier
                                    </label>
                                    <input
                                        id="classic-city-speed"
                                        className="scenario-setup__input"
                                        type="number"
                                        min="0.1"
                                        step="0.1"
                                        value={draft.speedMultiplier}
                                        onChange={(event) => updateDraft("speedMultiplier", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.speedMultiplier ? (
                                        <div className="scenario-setup__error">{validationErrors.speedMultiplier}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field scenario-setup__field--wide">
                                    <label className="scenario-setup__label" htmlFor="classic-city-seed">
                                        Generation seed
                                    </label>
                                    <input
                                        id="classic-city-seed"
                                        className="scenario-setup__input"
                                        value={draft.generationSeed}
                                        placeholder="Leave empty to derive a deterministic seed from launch inputs"
                                        onChange={(event) => updateDraft("generationSeed", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    <div className="scenario-setup__hint">
                                        Leaving this empty keeps the launch deterministic while still deriving the seed
                                        from the configured city profile.
                                    </div>
                                </div>
                            </div>

                            <OptionGrid
                                legend="City size"
                                options={CLASSIC_CITY_SIZE_TIER_OPTIONS}
                                selectedValue={draft.sizeTier}
                                onSelect={(value) => updateDraft("sizeTier", value)}
                                disabled={!canEditSession}
                            />

                            <OptionGrid
                                legend="Urban density"
                                options={CLASSIC_CITY_DENSITY_OPTIONS}
                                selectedValue={draft.urbanDensity}
                                onSelect={(value) => updateDraft("urbanDensity", value)}
                                disabled={!canEditSession}
                            />

                            <OptionGrid
                                legend="Development level"
                                options={CLASSIC_CITY_DEVELOPMENT_OPTIONS}
                                selectedValue={draft.developmentLevel}
                                onSelect={(value) => updateDraft("developmentLevel", value)}
                                disabled={!canEditSession}
                            />
                        </div>
                    ) : null}

                    {currentStep.id === "environment" ? (
                        <div className="scenario-setup__stack">
                            <OptionGrid
                                legend="Climate zone"
                                options={CLASSIC_CITY_CLIMATE_OPTIONS}
                                selectedValue={draft.climateZone}
                                onSelect={(value) => updateDraft("climateZone", value)}
                                disabled={!canEditSession}
                            />

                            <OptionGrid
                                legend="Hemisphere"
                                options={CLASSIC_CITY_HEMISPHERE_OPTIONS}
                                selectedValue={draft.hemisphere}
                                onSelect={(value) => updateDraft("hemisphere", value)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__form-grid">
                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-utc-offset">
                                        UTC offset (minutes)
                                    </label>
                                    <input
                                        id="classic-city-utc-offset"
                                        className="scenario-setup__input"
                                        type="number"
                                        step="15"
                                        value={draft.utcOffsetMinutes}
                                        onChange={(event) => updateDraft("utcOffsetMinutes", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.utcOffsetMinutes ? (
                                        <div className="scenario-setup__error">{validationErrors.utcOffsetMinutes}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <div className="scenario-setup__label">Weather bootstrap</div>
                                    <div className="scenario-setup__fact-card">
                                        <strong>Automatic from climate profile</strong>
                                        <span>
                                            Initial weather is still generated by CityCore from the climate setup and
                                            start simulation time. Manual weather tuning remains a follow-up slice,
                                            not a fake frontend-only field.
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ) : null}

                    {currentStep.id === "population" ? (
                        <div className="scenario-setup__stack">
                            <OptionGrid
                                legend="Population bootstrap mode"
                                options={CLASSIC_CITY_POPULATION_MODE_OPTIONS}
                                selectedValue={draft.populationMode}
                                onSelect={(value) => updateDraft("populationMode", value as ClassicCityPopulationMode)}
                                disabled={!canEditSession}
                            />

                            {draft.populationMode === "manual" ? (
                                <div className="scenario-setup__form-grid">
                                    <div className="scenario-setup__field">
                                        <label className="scenario-setup__label" htmlFor="classic-city-planned-people-count">
                                            Planned people count
                                        </label>
                                        <input
                                            id="classic-city-planned-people-count"
                                            className="scenario-setup__input"
                                            type="number"
                                            min="0"
                                            step="1"
                                            value={draft.plannedPeopleCount}
                                            onChange={(event) => updateDraft("plannedPeopleCount", event.target.value)}
                                            disabled={!canEditSession}
                                        />
                                        {validationErrors.plannedPeopleCount ? (
                                            <div className="scenario-setup__error">{validationErrors.plannedPeopleCount}</div>
                                        ) : null}
                                        <div className="scenario-setup__hint">
                                            This value is persisted into the setup session and CityCore generation profile, so
                                            bootstrap retry keeps the same resident target instead of silently falling back to
                                            automatic sizing.
                                        </div>
                                    </div>

                                    <div className="scenario-setup__field">
                                        <div className="scenario-setup__label">Capacity note</div>
                                        <div className="scenario-setup__fact-card">
                                            <strong>Manual target still respects generated housing capacity</strong>
                                            <span>
                                                Classic City topology is generated before population bootstrap. If the requested
                                                resident target exceeds the generated residential capacity, provisioning will cap
                                                the applied bootstrap count instead of overfilling the city.
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            ) : (
                                <div className="scenario-setup__fact-card">
                                    <strong>Automatic population sizing</strong>
                                    <span>
                                        Gateway will derive the initial resident target from the generated residential
                                        buildings, density profile, development level, and deterministic launch seed after the
                                        city skeleton is created.
                                    </span>
                                </div>
                            )}
                        </div>
                    ) : null}

                    {currentStep.id === "launch" ? (
                        <div className="scenario-setup__stack">
                            <div className="scenario-setup__review-grid">
                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">City identity</span>
                                    <strong className="scenario-setup__review-value">{draft.name || "Unnamed city"}</strong>
                                    <span className="scenario-setup__review-text">
                                        {draft.sizeTier} city, {draft.urbanDensity.toLowerCase()} density, {draft.developmentLevel.toLowerCase()} development.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Timeline</span>
                                    <strong className="scenario-setup__review-value">{draft.startSimTimeLocal || "--"}</strong>
                                    <span className="scenario-setup__review-text">
                                        Local launch input is persisted in the setup session together with the derived UTC timestamp.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Environment</span>
                                    <strong className="scenario-setup__review-value">
                                        {draft.climateZone} / {draft.hemisphere}
                                    </strong>
                                    <span className="scenario-setup__review-text">
                                        {formatUtcOffset(draft.utcOffsetMinutes)}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Population bootstrap</span>
                                    <strong className="scenario-setup__review-value">{getPopulationPlanLabel(draft.populationMode)}</strong>
                                    <span className="scenario-setup__review-text">
                                        {getPopulationPlanDescription(draft)}
                                    </span>
                                </article>
                            </div>

                            <div className="scenario-setup__note">
                                Launch is queued against the setup session and processed asynchronously by Gateway. If
                                anything fails before a city exists, the wizard stays resumable. If the city exists,
                                handoff moves to provisioning instead of pretending the launch was instantly complete.
                            </div>
                        </div>
                    ) : null}

                    <div className="scenario-setup__actions">
                        {currentStep.id === "scenario" ? (
                            <Link className="scenario-setup__secondary-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                                Choose another scenario
                            </Link>
                        ) : (
                            <Button
                                type="button"
                                variant="default"
                                onClick={goBack}
                                disabled={isBusy || !canEditSession}
                            >
                                Back
                            </Button>
                        )}

                        {currentStep.id !== "launch" ? (
                            <Button
                                type="button"
                                variant="primary"
                                onClick={goNext}
                                disabled={isBusy || !canEditSession}
                            >
                                Continue
                            </Button>
                        ) : (
                            <Button
                                type="button"
                                variant="success"
                                onClick={() => void handleLaunch()}
                                disabled={isBusy || !canEditSession}
                            >
                                {isLaunching ? "Queueing launch..." : "Launch Classic City"}
                            </Button>
                        )}
                    </div>
                </div>

                <aside className="scenario-setup__aside">
                    <div className={`scenario-setup__aside-card scenario-setup__aside-card--status-${sessionStatusTone}`}>
                        <div className="scenario-setup__aside-label">Setup session</div>
                        <div className="scenario-setup__aside-value">{sessionStatusLabel}</div>
                        <div className="scenario-setup__aside-list">
                            <div className="scenario-setup__aside-item">
                                <span>Draft</span>
                                <strong>{draft.name || "Classic City launch"}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Profile</span>
                                <strong>{draft.sizeTier} / {draft.urbanDensity} / {draft.developmentLevel}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Environment</span>
                                <strong>{draft.climateZone} / {draft.hemisphere}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Population</span>
                                <strong>{getPopulationPlanLabel(draft.populationMode)}</strong>
                                <span>{getPopulationPlanDescription(draft)}</span>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Clock</span>
                                <strong>{draft.speedMultiplier}x at {formatUtcOffset(draft.utcOffsetMinutes)}</strong>
                            </div>
                        </div>
                    </div>

                    <div className="scenario-setup__aside-card scenario-setup__aside-card--accent">
                        <div className="scenario-setup__aside-label">Persistence</div>
                        <p className="scenario-setup__aside-copy">
                            {isInitializing
                                ? "Preparing setup session..."
                                : isLaunching
                                    ? "Queueing launch request..."
                                    : isSaving
                                        ? "Saving draft to backend session..."
                                        : saveError
                                            ? "Autosave needs attention before launch can proceed cleanly."
                                            : lastSavedAtUtc
                                                ? `Last saved ${formatDateTime(lastSavedAtUtc)}`
                                                : "Draft has not been saved yet."}
                        </p>

                        {session?.launchQueuedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Launch queued</span>
                                <strong>{formatDateTime(session.launchQueuedAtUtc)}</strong>
                            </div>
                        ) : null}

                        {session?.startedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Provisioning started</span>
                                <strong>{formatDateTime(session.startedAtUtc)}</strong>
                            </div>
                        ) : null}

                        {session?.completedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Last completion</span>
                                <strong>{formatDateTime(session.completedAtUtc)}</strong>
                            </div>
                        ) : null}
                    </div>

                    <div className="scenario-setup__aside-card">
                        <div className="scenario-setup__aside-label">Operational note</div>
                        <p className="scenario-setup__aside-copy">
                            {getSessionStatusDescription(session)}
                        </p>

                        {isLaunchRunning ? (
                            <LoadingIndicator label="Refreshing setup session status..."/>
                        ) : null}
                    </div>
                </aside>
            </div>
        </section>
    );
}
